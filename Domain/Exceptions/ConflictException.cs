namespace Domain.Exceptions;

public class ConflictException : BusinessException
{
    public ConflictException(string message, object? details = null)
        : base("CONFLICT", message, details)
    {
        StatusCode = 409;
    }

    public ConflictException() : this("A conflicting resource already exists.") { }
}
