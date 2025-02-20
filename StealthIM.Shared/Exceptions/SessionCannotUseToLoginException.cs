using StealthIM.Shared.Models.Primitive;

namespace StealthIM.Shared.Exceptions;

public sealed class SessionCannotUseToLoginExceptionErrorInformationConverter : IErrorInformationConverter
{
    public Exception ConvertFrom(ErrorInformation errorInformation)
    {
        return new SessionCannotUseToLoginException(errorInformation.ErrorMessage.Split(": ")[1], errorInformation.Advice);
    }

    public ErrorInformation ConvertTo(Exception exception)
    {
        return new()
        {
            ErrorCode = 111,
            ErrorMessage = exception.Message.Split(" Advice: ")[0],
            Advice = exception.Message.Split(" Advice: ")[1]
        };
    }
}

[ExceptionDefaultErrorInformation(
    ErrorCode = 103,
    ErrorMessage = "SessionCannotUseToLoginException: Login session not found in session list or no usable.",
    Advice = "Use username and password to login."
)]
[ErrorInformationConverter(ConverterType = typeof(SessionCannotUseToLoginExceptionErrorInformationConverter))]
public class SessionCannotUseToLoginException(string message = "Login session not found in session list or no usable.", string advice = "Use username and password to login.") : Exception
{
    public override string Message => nameof(SessionCannotUseToLoginException) + message + " Advice: " + advice;
}
