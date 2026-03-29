namespace Domain.Exceptions;

public class ValidationException : BusinessException
{
    public ValidationException(string code, string message, object? details = null)
        : base(code, message, details)
    {
        StatusCode = 422;
    }

    public ValidationException(string message, object? details = null)
        : base("VALIDATION_ERROR", message, details)
    {
        StatusCode = 422;
    }

    public ValidationException() : this("A business rule was violated.")
    {
    }
}