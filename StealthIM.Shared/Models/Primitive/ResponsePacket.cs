using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace StealthIM.Shared.Models.Primitive;

public class ResponsePacket
{
    [JsonConverter(typeof(StringEnumConverter))]
    public ResponseType Type { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public RequestCommandType Command { get; set; }
    public ErrorInformation? ErrorInformation { get; set; }
    public object ExInformation { get; set; }
}
