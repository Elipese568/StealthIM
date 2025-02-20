using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StealthIM.Shared.Crypto;

public class AsciiCryptoProvider : ICryptoProvider
{
    public string Decrypt(byte[] data)
    {
        StringBuilder stringBuilder = new(data.Length);
        foreach (byte b in data)
        {
            stringBuilder.Append((char)b);
        }
        return stringBuilder.ToString();
    }

    public byte[] Encrypt(string data)
    {
        List<byte> result = new(data.Length);
        foreach(char c in data)
        {
            result.Add((byte)c);
        }
        return [.. result];
    }
}
