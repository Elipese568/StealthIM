using StealthIM.Shared.Models.Primitive;

namespace StealthIM.Shared.Exceptions;

public sealed class UserNotFoundInUserListExceptionErrorInformationConverter : IErrorInformationConverter
{
    public Exception ConvertFrom(ErrorInformation errorInformation)
    {
        return new UserNotFoundInUserListException(errorInformation.ErrorMessage.Split(": ")[1], errorInformation.Advice);
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
    ErrorCode = 111,
    ErrorMessage = "UserNotFoundInUserListException: Request username not found in user list.",
    Advice = "Try other username to be success."
)]
[ErrorInformationConverter(ConverterType = typeof(UserNotFoundInUserListExceptionErrorInformationConverter))]
public class UserNotFoundInUserListException(string message = "Request username not found in user list.", string advice = "Try other username to be success.") : Exception
{
    public override string Message => message + " Advice: " + advice;
}
