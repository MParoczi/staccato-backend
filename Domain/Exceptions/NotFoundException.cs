namespace Domain.Exceptions;

public class NotFoundException : BusinessException
{
    public NotFoundException(string message, object? details = null)
        : base("NOT_FOUND", message, details)
    {
        StatusCode = 404;
    }

    public NotFoundException() : this("The requested resource was not found.") { }
}
