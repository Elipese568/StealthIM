using Microsoft.Extensions.Logging;
using StealthIM.Shared;
using StealthIM.Shared.Exceptions;
using StealthIM.Shared.Models.Primitive;
using StealthIM.Shared.Models.Request;
using StealthIM.Shared.Models.Response;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace StealthIM.Server;
internal class Server
{
    private TcpListener listener;
    private CancellationToken _cancellationToken;

    private List<TcpClient> _unbindedClients = new();
    private Dictionary<TcpClient, User> _bindedUsers = new();


    private IPAddress _ipAddress;
    private int _port;

    private ILogger _logger;

    public Server(IPAddress ip, int port)
    {
        _logger = LoggerManager.GetLogger<Server>();
        try
        {
            listener = new(ip, port);
        }
        catch (ArgumentOutOfRangeException)
        {
            _logger.LogCritical("ArgumentOutOfRangeException: Port value out of range.");
            Environment.Exit(-1);
        }
        catch (ArgumentNullException)
        {
            _logger.LogCritical("ArgumentNullException: IPAddress is null.");
            Environment.Exit(-1);
        }
        _ipAddress = ip;
        _port = port;
    }

    public async Task Start(CancellationToken? cancellationToken = null)
    {
        _logger.LogDebug("Try start server on ip {IP} port {PORT}", _ipAddress, _port);
        try
        {
            listener.Start();
        }
        catch (SocketException e)
        {
            _logger.LogCritical("SocketException: " + e.ToString());
            Environment.Exit(-2);
        }
        _cancellationToken = cancellationToken ?? CancellationToken.None;
        while (!_cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Waiting for client");
            var client = await listener.AcceptTcpClientAsync(_cancellationToken);
            _logger.LogInformation("Client {IPORT} connected.", client.Client.RemoteEndPoint);
            AddClient(client);
        }
    }

    public async void AddClient(TcpClient client)
    {
        try
        {
            _unbindedClients.Add(client);
            var stream = client.GetStream();
            var user = await ReceiveLoginRequest(client.Client.RemoteEndPoint, stream);

            _bindedUsers.Add(client, user);
            _unbindedClients.Remove(client);
        }
        catch (IOException ioe)
        {
            _logger.LogInformation("Client {IPORT} trigger a unexcepted exception: {EXCEPTIONMESSAGE}, disconnected.", ioe.Message, client.Client.RemoteEndPoint);
            _bindedUsers.Remove(client);
            _bindedUsers.Remove(client);
        }
    }

    private async Task<User> ReceiveLoginRequest(EndPoint clientEndPoint, NetworkStream stream)
    {
        _logger.LogInformation("Waiting client {IPORT} for login or register request.", clientEndPoint);

        while (true)
        {
            var request = await LinkHelper.ReceiveRequestAsync(stream);
            User? result = default;
            switch (request.Command)
            {
                case Shared.Models.Primitive.RequestCommandType.Register:
                    _logger.LogInformation("Client {IPORT} send a register request.", clientEndPoint);
                    result = await HandleRegisterRequest(stream, request);
                    result?.Log(UserLogItemType.Register, $"Register: Register success. [Sender: {clientEndPoint}]");
                    break;
                case Shared.Models.Primitive.RequestCommandType.LoginByUnPw:
                    _logger.LogInformation("Client {IPORT} send a login request.", clientEndPoint);
                    result = await HandleLoginByUnPwRequest(stream, request);
                    if(result != null)
                    {
                        var loginByUnPwInfo = (LoginByUnPwRequestExInformation)request.ExInformation;
                        result?.Log(
                            UserLogItemType.Login,
                            $"Login: Login success. " +
                            $"[" +
                                $"Sender: {clientEndPoint}, " +
                                $"Method: UsernamePassword, " +
                                $"Username: {loginByUnPwInfo.Username}]," +
                                $"Password: {
                                    Convert.ToBase64String(
                                        SHA256.HashData(
                                            Encoding
                                                .UTF8
                                                .GetBytes(
                                                    loginByUnPwInfo.Password)
                                                )
                                        )
                                    }]");
                    }
                    break;
                case Shared.Models.Primitive.RequestCommandType.LoginBySession:
                    _logger.LogInformation("Client {IPORT} send a login request.", clientEndPoint);
                    result = await HandleLoginBySessionRequest(stream, request);
                    if (result != null)
                    {
                        var loginBySessionInfo = (LoginBySessionRequestExInformation)request.ExInformation;
                        result?.Log(
                            UserLogItemType.Login,
                            $"Login: Login success. " +
                            $"[" +
                                $"Sender: {clientEndPoint}, " +
                                $"Method: Session, " +
                                $"Session: {loginBySessionInfo.Session}]");
                    }
                    break;
                default:
                    _logger.LogWarning("Unknown request command from client {IPORT}.", clientEndPoint);
                    await LinkHelper.SendResponseAsync(stream, new()
                    {
                        Command = request.Command,
                        ErrorInformation = new RequestArgumentWrongExceptionErrorInformationConverter().ConvertTo(new RequestArgumentWrongException()),
                        ExInformation = null,
                        Type = ResponseType.Failure
                    });
                    break;
            }

            if(result != null)
                return result;
        }
    }

    private async Task<User?> HandleRegisterRequest(NetworkStream stream, RequestMessagePacket request)
    {
        var registerExInformation = request.ExInformation as RegisterRequestExInformation;
        var passwordSha = SHA256.HashData(Encoding.UTF8.GetBytes(registerExInformation.Password));

        _logger.LogInformation(
            """
             --------------------
             Register Information:
             Username:       {USERNAME}
             PasswordSha256: {PASSWORDSHA256}...
             Nickname:       {NICKNAME}
             --------------------
             """,
            registerExInformation.Username,
            Convert.ToBase64String(passwordSha)[..8],
            registerExInformation.Nickname);

        var registerStatus = UserManager.Instance.Register(registerExInformation.Username, Convert.ToBase64String(passwordSha), registerExInformation.Nickname);

        if (registerStatus.IsSuccess)
        {
            (bool samePassword, User user) = (ValueTuple<bool, User>)registerStatus.State;
            var response = new RegisterResponseExInformation()
            {
                LoginSession = user.Session.RawString,
                WarningSamePassword = samePassword,
                UserGuid = user.UserGuid
            };

            await LinkHelper.SendResponseAsync(stream, new()
            {
                Command = Shared.Models.Primitive.RequestCommandType.Register,
                Type = Shared.Models.Primitive.ResponseType.Success,
                ErrorInformation = Shared.Models.Primitive.ErrorInformation.Empty,
                ExInformation = response
            });

            return user;
        }
        else
        {
            await LinkHelper.SendResponseAsync(stream, new()
            {
                Command = Shared.Models.Primitive.RequestCommandType.Register,
                Type = Shared.Models.Primitive.ResponseType.Failure,
                ErrorInformation = registerStatus.ErrorInformation,
                ExInformation = null
            });
        }
        return null;
    }

    private async Task<User?> HandleLoginByUnPwRequest(NetworkStream stream, RequestMessagePacket request)
    {
        var loginExInformation = request.ExInformation as LoginByUnPwRequestExInformation;
        var passwordSha = SHA256.HashData(Encoding.UTF8.GetBytes(loginExInformation.Password));
        LoginResult loginResult = default;

        _logger.LogInformation(
            """
             --------------------
             Login Information:
             Username:       {USERNAME}
             PasswordSha256: {PASSWORDSHA256}...
             --------------------
             """,
            loginExInformation.Username,
            Convert.ToBase64String(passwordSha)[..8]);

        try
        {
            var loginStatus = UserManager.Instance.Login(loginExInformation.Username, Convert.ToBase64String(passwordSha));

            if (loginStatus.IsSuccess)
            {
                loginResult = (LoginResult)loginStatus.State;
                loginResult.Confirmer.Confirm();

                var response = new LoginResponseExInformation()
                {
                    LoginSession = loginResult.ResultUser.Session.RawString,
                    UserGuid = loginResult.ResultUser.UserGuid
                };

                await LinkHelper.SendResponseAsync(stream, new()
                {
                    Command = Shared.Models.Primitive.RequestCommandType.LoginByUnPw,
                    Type = Shared.Models.Primitive.ResponseType.Success,
                    ErrorInformation = Shared.Models.Primitive.ErrorInformation.Empty,
                    ExInformation = response
                });
                return loginResult.ResultUser;
            }
            else
            {
                await LinkHelper.SendResponseAsync(stream, new()
                {
                    Command = Shared.Models.Primitive.RequestCommandType.LoginByUnPw,
                    Type = Shared.Models.Primitive.ResponseType.Failure,
                    ErrorInformation = loginStatus.ErrorInformation,
                    ExInformation = null
                });
            }
            return null;
        }
        catch
        {
            if(loginResult.Confirmer != null)
                loginResult.Confirmer.Cancel();

            return null;
        }
    }

    private async Task<User?> HandleLoginBySessionRequest(NetworkStream stream, RequestMessagePacket request)
    {
        var loginExInformation = request.ExInformation as LoginBySessionRequestExInformation;
        LoginResult loginResult = default;

        _logger.LogInformation(
            """
             --------------------
             Login Information:
             LoginSession:   {SESSION}
             --------------------
             """,
            loginExInformation.Session);

        try
        {
            var loginStatus = UserManager.Instance.Login(loginExInformation.Session);

            if (loginStatus.IsSuccess)
            {
                loginResult = (LoginResult)loginStatus.State;
                loginResult.Confirmer.Confirm();

                var response = new LoginResponseExInformation()
                {
                    LoginSession = loginResult.ResultUser.Session.RawString,
                    UserGuid = loginResult.ResultUser.UserGuid
                };

                await LinkHelper.SendResponseAsync(stream, new()
                {
                    Command = Shared.Models.Primitive.RequestCommandType.LoginBySession,
                    Type = Shared.Models.Primitive.ResponseType.Success,
                    ErrorInformation = Shared.Models.Primitive.ErrorInformation.Empty,
                    ExInformation = response
                });
                return loginResult.ResultUser;
            }
            else
            {
                await LinkHelper.SendResponseAsync(stream, new()
                {
                    Command = Shared.Models.Primitive.RequestCommandType.LoginBySession,
                    Type = Shared.Models.Primitive.ResponseType.Failure,
                    ErrorInformation = loginStatus.ErrorInformation,
                    ExInformation = null
                });
            }
            return null;
        }
        catch
        {
            if (loginResult.Confirmer != null)
                loginResult.Confirmer.Cancel();

            return null;
        }
    }
}
