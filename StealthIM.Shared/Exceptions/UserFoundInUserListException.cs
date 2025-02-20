using StealthIM.Shared.Models.Primitive;

namespace StealthIM.Shared.Exceptions;

public sealed class UserFoundInUserListExceptionErrorInformationConverter : IErrorInformationConverter
{
    public Exception ConvertFrom(ErrorInformation errorInformation)
    {
        return new UserFoundInUserListException(errorInformation.ErrorMessage.Split(": ")[1], errorInformation.Advice);
    }

    public ErrorInformation ConvertTo(Exception exception)
    {
        return new()
        {
            ErrorCode = 101,
            ErrorMessage = exception.Message.Split(" Advice: ")[0],
            Advice = exception.Message.Split(" Advice: ")[1]
        };
    }
}

[ExceptionDefaultErrorInformation(
    ErrorCode = 101,
    ErrorMessage = "UserFoundInUserListException: Request username found in user list.",
    Advice = "Try other username to be success."
)]
[ErrorInformationConverter(ConverterType = typeof(UserFoundInUserListExceptionErrorInformationConverter))]
public class UserFoundInUserListException(string message = "Request username found in user list.", string advice = "Try other username to be success.") : Exception
{
    public override string Message => nameof(UserFoundInUserListException) + message + " Advice: " + advice;
}
