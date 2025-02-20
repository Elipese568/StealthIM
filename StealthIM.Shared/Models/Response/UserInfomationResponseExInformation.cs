namespace StealthIM.Shared.Models.Response;

public class UserInformationResponseExInformation
{
    public string Nickname { get; set; }
    public string Username { get; set; }
    public DateTime RegisterTime { get; set; }
    public DateTime LastLoginTime { get; set; }
    public Dictionary<string, string> Other { get; set; }
}
