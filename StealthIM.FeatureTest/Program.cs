using StealthIM.Shared;
using StealthIM.Shared.Crypto;
using StealthIM.Shared.Models.Primitive;
using StealthIM.Shared.Models.Request;
using StealthIM.Shared.Models.Response;
using System.Net.Sockets;

namespace StealthIM.FeatureTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", 11451);
            var stream = client.GetStream();
            LinkHelper.SetCryptoProvider(new AsciiCryptoProvider());
            Console.WriteLine("Input Session to login:");

            LinkHelper.SendRequest(stream, new RequestMessagePacket()
            {
                Command = RequestCommandType.Register,
                ExInformation = new RegisterRequestExInformation()
                {
                    Nickname = "Test",
                    Password = "Test",
                    Username = "Test"
                }
            });

            var reponse = LinkHelper.ReceiveResponse(stream);
            //var rpex = reponse.ExInformation as LoginResponseExInformation;
            //Console.WriteLine("UserGuid: " + rpex.UserGuid);
            //Console.WriteLine("LoginSession: " + rpex.LoginSession);
            Console.ReadKey();
        }
    }
}
