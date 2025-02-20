using System.Text;

namespace StealthIM.Shared.Crypto;

public class NonCryptoProvider : ICryptoProvider
{
    public string Decrypt(byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    public byte[] Encrypt(string data)
    {
        return Encoding.UTF8.GetBytes(data);
    }
}
