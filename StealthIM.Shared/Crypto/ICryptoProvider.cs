namespace StealthIM.Shared.Crypto;

public interface ICryptoProvider
{
    public byte[] Encrypt(string data);
    public string Decrypt(byte[] data);
}
