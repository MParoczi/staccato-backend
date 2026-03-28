namespace Domain.Exceptions;

public class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string code, string message, object? details = null)
        : base(code, message, details)
    {
        StatusCode = 401;
    }

    public UnauthorizedException(string message, object? details = null)
        : base("UNAUTHORIZED", message, details)
    {
        StatusCode = 401;
    }

    public UnauthorizedException() : this("UNAUTHORIZED", "Authentication failed.")
    {
    }
}