using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StealthIM.Shared.Models.Primitive;

public class RequestMessagePacket
{
    [JsonConverter(typeof(StringEnumConverter))]
    public RequestCommandType Command { get; set; }
    public object ExInformation { get; set; }
}
