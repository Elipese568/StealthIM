using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StealthIM.Shared.Exceptions;
using System.Text;

namespace StealthIM.Server;

public record struct Session(string RawString, DateTime GenerationTime, DateTime UsableLeastTime)
{
    public static Session MakeSession()
    {
        StringBuilder sb = new();
        Random random = new((int)(DateTime.Now.Ticks << 32 >> 32));

        for (int i = 0; i < 16; i++)
        {
            char current;
            do
            {
                current = (char)random.Next('0', 'z');
            }
            while (!(char.IsDigit(current) || char.IsLetter(current)));
            sb.Append(current);
        }

        return new(
            sb.ToString(),
            DateTime.Now,
            DateTime.Now + TimeSpan.FromDays(30.0));
    }

    public static bool Check(Session session)
    {
        return DateTime.Now < session.UsableLeastTime;
    }
}

public enum UserLogItemType
{
    Register,
    Login
}

public record struct UserLogItem(DateTime RecordTime, UserLogItemType Type, string Message);

public record class User(
    string Username,
    string Nickname,
    string PasswordSHA256,
    Guid UserGuid,
    Session Session,
    DateTime LastLoginTime,
    DateTime RegisterTime,
    List<UserLogItem> UserLog,
    Dictionary<string, string> OtherInformation)
{
    public void Log(UserLogItemType type, string message)
    {
        UserLog.Add(new UserLogItem(DateTime.Now, type, message));
    }
}

public sealed class LoginUserConfirmer
{
    private readonly List<User> _users;
    private readonly int _index;
    private readonly ILogger _logger;
    private User _target;
    private bool _operated = false;

    public LoginUserConfirmer(List<User> users, User target)
    {
        _index = users.FindIndex(x => x.UserGuid == target.UserGuid);
        _users = users;
        _target = target;
        _logger = LoggerManager.GetLogger<LoginUserConfirmer>();
    }

    public User GetCurrent() => _target;

    public void Confirm()
    {
        if (_operated)
        {
            return;
        }
        _target = _target with { LastLoginTime = DateTime.Now, Session = Session.MakeSession() };

        _logger.LogInformation("Login session refreshed. [Username: {USERNAME}, UserGuid: {USERGUID}]", _target.Username, _target.UserGuid);
        _logger.LogInformation("Login success. [Username: {USERNAME}, UserGuid: {USERGUID}]", _target.Username, _target.UserGuid);
        _target.Log(UserLogItemType.Login, "Login session refreshed.");
        _target.Log(UserLogItemType.Login, "Login success.");
        _users[_index] = _target;
        _operated = true;
    }

    public void Cancel()
    {
        if (_operated)
        {
            return;
        }
        _logger.LogInformation("Login failed. [Username: {USERNAME}, UserGuid: {USERGUID}]", _target.Username, _target.UserGuid);
        _target.Log(UserLogItemType.Login, "Login failed.");
        _operated = true;
    }
}

public readonly struct LoginResult
{
    public readonly User ResultUser => Confirmer.GetCurrent();
    public readonly LoginUserConfirmer Confirmer { get; init; }
}

public class UserManager
{
    private List<User> _users = new List<User>();
    private ILogger _logger;

    private static UserManager _instance;
    public static UserManager Instance
    {
        get
        {
            _instance ??= new UserManager();
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }

    public UserManager()
    {
        _logger = LoggerManager.GetLogger<UserManager>();

        _logger.LogInformation("Load user data in UserData.json");
        if (!File.Exists("UserData.json"))
        {
            _logger.LogInformation("Do not exist UserData.json. Now is creating.");
            File.Create("UserData.json");
            _users = [];
            return;
        }
        else
        {
            _logger.LogInformation("Existing UserData.json. Now is loading.");
            DateTime startLoad = DateTime.Now;
            _users = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText("UserData.json")) ?? [];
            _logger.LogInformation("Load {COUNT} users data from UserData.json, in {MS} ms", _users.Count, (DateTime.Now - startLoad).TotalMilliseconds);
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            _logger.LogInformation("Application exit, saving user data...");
            File.WriteAllText("UserData.json", JsonConvert.SerializeObject(_users, Formatting.Indented));
            _logger.LogInformation("Saved {COUNT} users data to UserData.json", _users.Count);
        };
    }

    public Status Register(string username, string passwordSHA256, string nickname)
    {
        _logger.LogInformation("Start registing...");
        _logger.LogInformation("Checking there is existing username or not...");
        if (_users.Any(x => x.Username == username))
        {
            _logger.LogError("UserFoundInUserListException: There is a username same of {USERNAME}", username);
            return Status.Failed(new UserFoundInUserListException());
        }

        _logger.LogInformation("Checking there is existing password or not...");
        bool samePassword = _users.Any(x => x.PasswordSHA256 == passwordSHA256);
        if (samePassword)
        {
            _logger.LogWarning("SamePasswordWarning: There is a password same of {PASSWORD}", passwordSHA256[..8]);
        }

        User registerUser = new(
            username,
            nickname,
            passwordSHA256,
            Guid.NewGuid(),
            Session.MakeSession(),
            DateTime.Now,
            DateTime.Now,
            [],
            []);
        _users.Add(registerUser);

        _logger.LogInformation("Register is success.");
        _logger.LogInformation(
            """
             --------------------
             Registed user Information:
             Username:       {USERNAME}
             PasswordSha256: {PASSWORDSHA256}...
             Nickname:       {NICKNAME}
             UserGuid:       {USERGUID}
             Session:
                GenerationTime:    {GENERATIONTIME}
                UsableLeastTime:   {USABLELEASTTIME}
             --------------------
             """,
            username,
            passwordSHA256,
            nickname,
            registerUser.UserGuid,
            registerUser.Session.GenerationTime,
            registerUser.Session.UsableLeastTime);

        

        return Status.Success((samePassword, registerUser));
    }

    public Status Login(string username, string passwordSHA256)
    {
        _logger.LogInformation("Start logining...");
        _logger.LogInformation("Login method: Username and Password");
        _logger.LogInformation("Checking there is existing username or not...");
        if (!_users.Any(x => x.Username == username))
        {
            _logger.LogError("There is not a username same of {USERNAME}", username);
            return Status.Failed(new UserNotFoundInUserListException());
        }

        _logger.LogInformation("Finding user of {USERNAME}...", username);

        var userindex = _users.FindIndex(x => x.Username == username);
        var user = _users[userindex];

        _logger.LogInformation(
            """
             --------------------
             Login Information:
             TargetUser:
                Username: {USERNAME}
                UserGuid: {USERGUID}
                Nickname: {NICKNAME}
             LoginPassword: {PASSWORDSHA256}
             --------------------
             """,
            user.Username,
            user.UserGuid,
            user.Nickname,
            passwordSHA256);

        if (user.PasswordSHA256 != passwordSHA256)
        {
            _logger.LogError("Login failed.");
            _logger.LogError("PasswordWrongException: Password of request is wrong of target user.");
            return Status.Failed(new PasswordWrongException());
        }

        _logger.LogInformation("Login maybe success.");

        return Status.Success(new LoginResult()
        {
            Confirmer = new(_users, user)
        });
    }

    public Status Login(string sessionRawString)
    {
        
        _logger.LogInformation("Start logining...");
        _logger.LogInformation("Login method: Session");

        var userindex = _users.FindIndex(x => x.Session.RawString == sessionRawString);
        if(userindex == -1)
        {
            _logger.LogError("Login failed.");
            _logger.LogError("SessionCannotUseToLoginException: Login session not found in session list or no usable.");
            return Status.Failed(new SessionCannotUseToLoginException());
        }
        var user = _users[userindex];

        _logger.LogInformation(
            """
             --------------------
             Login Information:
             TargetUser:
                Username: {USERNAME}
                UserGuid: {USERGUID}
                Nickname: {NICKNAME}
             LoginSession: {SESSION}
             --------------------
             """,
            user.Username,
            user.UserGuid,
            user.Nickname,
            sessionRawString);

        if (user.Session.RawString != sessionRawString || DateTime.Now > user.Session.UsableLeastTime)
        {
            _logger.LogError("Login failed.");
            _logger.LogError("SessionCannotUseToLoginException: Login session not found in session list or no usable.");
            return Status.Failed(new SessionCannotUseToLoginException());
        }

        _logger.LogInformation("Login maybe success.");

        return Status.Success(new LoginResult()
        {
            Confirmer = new(_users, user)
        });
    }


    public Status GetUserInformation(Guid userGuid)
    {
        _logger.LogInformation("Start getting user information...");
        _logger.LogInformation("Checking there is existing UserGuid or not...");
        if (!_users.Any(x => x.UserGuid == userGuid))
        {
            _logger.LogError("UserNotFoundInUserListException: There is not a UserGuid same of {USERGUID}", userGuid);
            return Status.Failed(new UserNotFoundInUserListException("Request UserGuid not found in user list.", "Correct your request UserGuid."), false);
        }
        _logger.LogInformation("Finding user of {USERGUID}...", userGuid);
        var user = _users.Find(x => x.UserGuid == userGuid);
        _logger.LogInformation(
            """
             --------------------
             User Information:
             TargetUser:
                Username: {USERNAME}
                UserGuid: {USERGUID}
                Nickname: {NICKNAME}
                RegisterTime: {REGISTERTIME}
                LastLoginTime: {LASTLOGINTIME},
                OtherInformation: {OTHER}
             --------------------
             """,
            user.Username,
            user.UserGuid,
            user.Nickname,
            user.RegisterTime,
            user.LastLoginTime,
            user.OtherInformation.StringDescriptor());
        return Status.Success(user);
    }
}
