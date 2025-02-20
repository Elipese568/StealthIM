using StealthIM.Shared.Models.Primitive;

namespace StealthIM.Shared.Exceptions;

[AttributeUsage(AttributeTargets.Class)]
public class ExceptionDefaultErrorInformationAttribute : Attribute
{
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public string Advice { get; set; }

    public ErrorInformation AsErrorInformation() => new()
    {
        ErrorCode = ErrorCode,
        ErrorMessage = ErrorMessage,
        Advice = Advice
    };
}
