namespace StealthIM.Shared.Models.Response;

public class RegisterResponseExInformation
{
    public bool WarningSamePassword { get; set; }
    public Guid UserGuid { get; set; }
    public string LoginSession { get; set; }
}
