namespace Domain.Exceptions;

public class BadRequestException : BusinessException
{
    public BadRequestException(string code, string message, object? details = null)
        : base(code, message, details)
    {
        StatusCode = 400;
    }
}
