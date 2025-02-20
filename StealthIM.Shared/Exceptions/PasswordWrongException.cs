using StealthIM.Shared.Models.Primitive;

namespace StealthIM.Shared.Exceptions;

public sealed class PasswordWrongExceptionErrorInformationConverter : IErrorInformationConverter
{
    public Exception ConvertFrom(ErrorInformation errorInformation)
    {
        return new PasswordWrongException(errorInformation.ErrorMessage.Split(": ")[1], errorInformation.Advice);
    }

    public ErrorInformation ConvertTo(Exception exception)
    {
        return new()
        {
            ErrorCode = 102,
            ErrorMessage = exception.Message.Split(" Advice: ")[0],
            Advice = exception.Message.Split(" Advice: ")[1]
        };
    }
}

[ExceptionDefaultErrorInformation(
    ErrorCode = 102,
    ErrorMessage = "PasswordWrongException: Password of request is wrong of target user.",
    Advice = "Try other password."
)]
[ErrorInformationConverter(ConverterType = typeof(PasswordWrongExceptionErrorInformationConverter))]
public class PasswordWrongException(string message = "Password of request is wrong of target user.", string advice = "Try other password.") : Exception
{
    public override string Message => nameof(PasswordWrongException) + message + " Advice: " + advice;
}
