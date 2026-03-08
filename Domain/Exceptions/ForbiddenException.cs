namespace Domain.Exceptions;

public class ForbiddenException : BusinessException
{
    public ForbiddenException(string message, object? details = null)
        : base("FORBIDDEN", message, details)
    {
        StatusCode = 403;
    }

    public ForbiddenException() : this("You do not have access to this resource.")
    {
    }
}