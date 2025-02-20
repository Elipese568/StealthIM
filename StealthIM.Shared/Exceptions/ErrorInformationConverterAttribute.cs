namespace StealthIM.Shared.Exceptions;

[AttributeUsage(AttributeTargets.Class)]
public class ErrorInformationConverterAttribute : Attribute
{
    public Type ConverterType { get; set; }
}
