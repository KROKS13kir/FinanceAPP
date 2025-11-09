namespace Finance.Domain.Errors;

internal sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
