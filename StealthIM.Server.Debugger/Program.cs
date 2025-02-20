using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StealthIM.Shared;
using StealthIM.Shared.Crypto;
using StealthIM.Shared.Models.Primitive;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace StealthIM.Server.Debugger;

internal class Program
{
    static (Stream, TcpClient) Client()
    {
        var _ = """
            127.0.0.1
            11451
            Ascii
            UserSetting

            """;
        Console.WriteLine("Input Server IP");
        string ip = Console.ReadLine();
        Console.Clear();
        Console.WriteLine("Input Server Port");
        int port = int.Parse(Console.ReadLine());
        Console.Clear();
        TcpClient client = new TcpClient();
        client.Connect(ip, port);
        var stream = client.GetStream();
        return (stream, client);
    }

    static ICryptoProvider CryptoProvider()
    {
        Console.Clear();

        Dictionary<string, Type> providers = new()
        {
            ["Ascii"] = typeof(AsciiCryptoProvider),
            ["Non"] = typeof(NonCryptoProvider)
        };

        Console.WriteLine("Select Crypto Provider:");
        foreach (var provider in providers)
        {
            Console.WriteLine(provider.Key);
        }
        if (!providers.TryGetValue(Console.ReadLine(), out Type providerType))
        {
            Console.WriteLine("Invalid Provider");
            return CryptoProvider();
        }
        return (ICryptoProvider)Activator.CreateInstance(providerType);
    }

    static RequestCommandType InputCommandType()
    {
        Console.Clear();
        Console.WriteLine("Input Command:");
        string command = Console.ReadLine();
        if (!Enum.TryParse<RequestCommandType>(command, out RequestCommandType commandType))
        {
            Console.WriteLine("Invalid Command");
            return InputCommandType();
        }
        return commandType;
    }

    static object ConstructExInformation(RequestCommandType command)
    {
        Type exInformationType = LinkHelper.CommandsExInformationTypeMap[command];
        object exInformation = Activator.CreateInstance(exInformationType);

        Dictionary<Type, Func<string, object>> converters = new()
        {
            [typeof(string)] = (string input) => input,
            [typeof(int)] = (string input) => int.Parse(input),
            [typeof(Guid)] = (string input) => Guid.Parse(input),
            [typeof(bool)] = (string input) => bool.Parse(input),
            [typeof(Dictionary<string, string>)] = (string input) => JsonConvert.DeserializeObject<Dictionary<string, string>>(input)
        };
        foreach (var property in exInformationType.GetProperties())
        {
            Start:
            try
            {
                Console.Write($"Input {property.Name}({property.PropertyType}): ");
                property.SetValue(exInformation, converters[property.PropertyType](Console.ReadLine()));
            }
            catch
            {
                Console.WriteLine("Invalid Input");
                goto Start;
            }
        }
        return exInformation;
    }

    static RequestMessagePacket ConstructPacket()
    {
        Console.Clear();
        var command = InputCommandType();

        Type exInformationType = LinkHelper.CommandsExInformationTypeMap[command];
        object exInformation = ConstructExInformation(command);

        return new RequestMessagePacket()
        {
            Command = command,
            ExInformation = exInformation
        };
    }
    static void Main(string[] args)
    {
        (Stream stream, TcpClient client) = Client();
        ICryptoProvider cryptoProvider = CryptoProvider();
        LinkHelper.SetCryptoProvider(cryptoProvider);

        while(true)
        {
            var packet = ConstructPacket();
        }
    }
}
