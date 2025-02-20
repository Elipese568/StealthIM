namespace StealthIM.Shared.Models.Primitive;

public class ErrorInformation
{
    public string ErrorMessage { get; set; }
    public int ErrorCode { get; set; }
    public string Advice { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(ErrorCode, Advice, ErrorCode);
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj == this) return true;
        if (obj.GetType() != typeof(ErrorInformation)) return false;
        return obj.GetHashCode() == GetHashCode();
    }

    public readonly static ErrorInformation Empty = new()
    {
        Advice = "",
        ErrorCode = 0,
        ErrorMessage = ""
    };
}
