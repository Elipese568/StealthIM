using Microsoft.Extensions.Logging;
using StealthIM.Shared;
using StealthIM.Shared.Crypto;
using System.Net;

namespace StealthIM.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        Setting.Initialize();
        LinkHelper.SetCryptoProvider(new AsciiCryptoProvider());

        Server si = new(IPAddress.Parse(Setting.Get("ServerIP", "127.0.0.1")), int.Parse(Setting.Get("ServerPort", "11451")));
        si.Start();
        Console.ReadLine();
    }
}
