namespace StealthIM.Shared.Models.Request;

public class UserSettingRequestExInformation
{
    public Guid UserGuid { get; set; }
    public string Nickname { get; set; }
    public Dictionary<string, string> Other { get; set; }
}
