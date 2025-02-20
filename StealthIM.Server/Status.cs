using StealthIM.Shared.Exceptions;
using StealthIM.Shared.Models.Primitive;
using System.Reflection;

namespace StealthIM.Server;

public struct Status
{
    public bool IsSuccess { get; set; }
    public ErrorInformation ErrorInformation { get; set; }
    public Exception? UnderlyingException { get; set; }
    public object? State { get; set; }

    public static Status Failed(Exception exception, bool useDefault = true, object? state = null)
    {
        if (exception.GetType().GetCustomAttribute<ExceptionDefaultErrorInformationAttribute>() is ExceptionDefaultErrorInformationAttribute attr && useDefault)
        {
            return new Status()
            {
                IsSuccess = false,
                ErrorInformation = attr.AsErrorInformation(),
                UnderlyingException = exception,
                State = state
            };
        }
        return new()
        {
            IsSuccess = false,
            ErrorInformation =
                exception
                    .GetType()
                    .GetCustomAttribute<ErrorInformationConverterAttribute>()
                    is
                    ErrorInformationConverterAttribute converterAttr ?
                        ((IErrorInformationConverter)Activator.CreateInstance(converterAttr.ConverterType)).ConvertTo(exception)
                    :
                        new ErrorInformation()
                        {
                            Advice = exception.HelpLink,
                            ErrorCode = exception.GetHashCode(),
                            ErrorMessage = exception.Message.ToString()
                        },
            UnderlyingException = exception,
            State = state
        };
    }

    public static Status Failed(ErrorInformation errorInformation, Exception? underlyingException = null, object? state = null)
    {
        return new()
        {
            IsSuccess = false,
            ErrorInformation = errorInformation,
            UnderlyingException = underlyingException,
            State = state
        };
    }

    public static Status Success(object? state = null) => new() { IsSuccess = true, State = state };
}
