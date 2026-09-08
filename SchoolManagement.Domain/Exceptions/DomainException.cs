namespace SchoolManagement.Domain.Exceptions;

/// <summary>
/// Raised when a business rule is violated. The presentation layer surfaces the
/// message directly to the user, so messages must stay end user readable.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
