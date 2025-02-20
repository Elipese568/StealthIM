using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StealthIM.Shared.Crypto;
using StealthIM.Shared.Models.Primitive;
using StealthIM.Shared.Models.Request;
using StealthIM.Shared.Models.Response;
using System.Net;
using System.Net.Sockets;

namespace StealthIM.Shared;

public class LinkHelper
{
    private static ICryptoProvider _cryptoProvider;
    public readonly static Dictionary<RequestCommandType, Type> CommandsExInformationTypeMap = new()
    {
        [RequestCommandType.Register] = typeof(RegisterRequestExInformation),
        [RequestCommandType.LoginByUnPw] = typeof(LoginByUnPwRequestExInformation),
        [RequestCommandType.LoginBySession] = typeof(LoginBySessionRequestExInformation),
        [RequestCommandType.ChangePassword] = typeof(ChangePasswordRequestExInformation),
        [RequestCommandType.GetUserInformation] = typeof(GetUserInformationRequestExInformation),
        [RequestCommandType.Unregister] = typeof(UnregisterRequestExInformation),
        [RequestCommandType.UserSetting] = typeof(UserSettingRequestExInformation)
    };

    public readonly static Dictionary<RequestCommandType, Type> ResponseExInformationTypeMap = new()
    {
        [RequestCommandType.Register] = typeof(RegisterResponseExInformation),
        [RequestCommandType.LoginBySession] = typeof(LoginResponseExInformation),
        [RequestCommandType.LoginByUnPw] = typeof(LoginResponseExInformation),
        [RequestCommandType.UserSetting] = typeof(UserInformationResponseExInformation),
        [RequestCommandType.GetUserInformation] = typeof(UserInformationResponseExInformation),
        [RequestCommandType.Unregister] = Type.Missing.GetType(),
        [RequestCommandType.ChangePassword] = Type.Missing.GetType()
    };

    public static byte[] GetNetworkByteArray(byte[] orig)
    {
        if (BitConverter.IsLittleEndian)
        {
            byte[] data = new byte[orig.Length];
            orig.CopyTo(data, 0);
            Array.Reverse(data, 0, orig.Length);
            return data;
        }
        return orig;
    }

    public static byte[] GetHostByteArray(byte[] network)
    {
        if (BitConverter.IsLittleEndian)
        {
            byte[] data = new byte[network.Length];
            network.CopyTo(data, 0);
            Array.Reverse(data, 0, network.Length);
            return data;
        }
        return network;
    }

    public static void SetCryptoProvider(ICryptoProvider cryptoProvider)
    { _cryptoProvider = cryptoProvider; }

    public static RequestMessagePacket BytesToRequestPacket(byte[] data)
    {
        string packetJsonContent = _cryptoProvider.Decrypt(data);
        RequestMessagePacket packet = JsonConvert.DeserializeObject<RequestMessagePacket>(packetJsonContent);

        packet.ExInformation = ((JObject)packet.ExInformation).ToObject(CommandsExInformationTypeMap[packet.Command]);
        return packet;
    }

    public static async Task<RequestMessagePacket> ReceiveRequestAsync(NetworkStream stream)
    {
        byte[] lengthBuffer = new byte[4];
        await stream.ReadAsync(lengthBuffer.AsMemory());

        int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer));

        byte[] buffer = new byte[length];
        await stream.ReadAsync(buffer.AsMemory(0, length));

        return BytesToRequestPacket(GetHostByteArray(buffer));
    }

    public static RequestMessagePacket ReceiveRequest(NetworkStream stream)
        => ReceiveRequestAsync(stream).GetAwaiter().GetResult();

    public static ResponsePacket BytesToResponsePacket(byte[] data)
    {
        string packetJsonContent = _cryptoProvider.Decrypt(data);
        ResponsePacket packet = JsonConvert.DeserializeObject<ResponsePacket>(packetJsonContent);

        if (ResponseExInformationTypeMap.TryGetValue(packet.Command, out var ExIT) && ExIT != Type.Missing.GetType())
            if (packet.ErrorInformation.Equals(ErrorInformation.Empty))
                packet.ExInformation = ((JObject)packet.ExInformation).ToObject(ExIT);
            else
                packet.ExInformation = null;
        else
            packet.ExInformation = null;
        return packet;
    }

    public static async Task<ResponsePacket> ReceiveResponseAsync(NetworkStream stream)
    {
        byte[] lengthBuffer = new byte[4];
        await stream.ReadAsync(lengthBuffer.AsMemory());

        int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer));

        byte[] buffer = new byte[length];
        await stream.ReadAsync(buffer.AsMemory(0, length));

        return BytesToResponsePacket(GetHostByteArray(buffer));
    }

    public static ResponsePacket ReceiveResponse(NetworkStream stream)
        => ReceiveResponseAsync(stream).GetAwaiter().GetResult();

    public static byte[] RequestPacketToBytes(RequestMessagePacket packet)
    {
        string packetJsonContent = JsonConvert.SerializeObject(packet);
        var bytes = _cryptoProvider.Encrypt(packetJsonContent);
        return GetNetworkByteArray(bytes);
    }

    public static async Task SendRequestAsync(NetworkStream stream, RequestMessagePacket packet)
    {
        var data = RequestPacketToBytes(packet);
        int length = IPAddress.HostToNetworkOrder(data.Length);
        await stream.WriteAsync(BitConverter.GetBytes(length));

        await stream.WriteAsync(data);
    }

    public static byte[] ResponsePacketToBytes(ResponsePacket packet)
    {
        string packetJsonContent = JsonConvert.SerializeObject(packet);
        var bytes = _cryptoProvider.Encrypt(packetJsonContent);
        return GetNetworkByteArray(bytes);
    }

    public static async Task SendResponseAsync(NetworkStream stream, ResponsePacket packet)
    {
        packet.ErrorInformation ??= ErrorInformation.Empty;

        var data = ResponsePacketToBytes(packet);
        int length = IPAddress.HostToNetworkOrder(data.Length);
        await stream.WriteAsync(BitConverter.GetBytes(length));

        await stream.WriteAsync(data);
    }

    public static void SendRequest(NetworkStream stream, RequestMessagePacket packet)
        => SendRequestAsync(stream, packet).GetAwaiter().GetResult();

    public static void SendResponse(NetworkStream stream, ResponsePacket packet)
        => SendResponseAsync(stream, packet).GetAwaiter().GetResult();
}
