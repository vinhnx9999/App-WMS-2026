namespace WMS.Domain.Common;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message)
        : base(message)
    {
        Code = "DOMAIN_ERROR";
    }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
