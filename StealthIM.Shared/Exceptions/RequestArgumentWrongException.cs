using StealthIM.Shared.Models.Primitive;

namespace StealthIM.Shared.Exceptions;

public sealed class RequestArgumentWrongExceptionErrorInformationConverter : IErrorInformationConverter
{
    public Exception ConvertFrom(ErrorInformation errorInformation)
    {
        return new RequestArgumentWrongException(errorInformation.ErrorMessage.Split(": ")[1], errorInformation.Advice);
    }

    public ErrorInformation ConvertTo(Exception exception)
    {
        return new()
        {
            ErrorCode = 103,
            ErrorMessage = exception.Message.Split(" Advice: ")[0],
            Advice = exception.Message.Split(" Advice: ")[1]
        };
    }
}

[ExceptionDefaultErrorInformation(
    ErrorCode = 103,
    ErrorMessage = "RequestArgumentWrongException: Argument(s) of request is wrong.",
    Advice = "Please correct your argument."
)]
[ErrorInformationConverter(ConverterType = typeof(RequestArgumentWrongExceptionErrorInformationConverter))]
public class RequestArgumentWrongException(string message = "Argument(s) of request is wrong.", string advice = "Please correct your argument.") : Exception
{
    public override string Message => nameof(RequestArgumentWrongException) + message + " Advice: " + advice;
}
