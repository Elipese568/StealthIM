using StealthIM.Shared.Models.Primitive;

namespace StealthIM.Shared.Exceptions;

public interface IErrorInformationConverter
{
    public ErrorInformation ConvertTo(Exception exception);
    public Exception ConvertFrom(ErrorInformation errorInformation);
}
