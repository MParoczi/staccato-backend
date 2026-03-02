namespace Domain.Exceptions;

public abstract class BusinessException : Exception
{
    protected BusinessException(string code, string message, object? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }

    public string Code { get; }
    public int StatusCode { get; protected init; } = 422;
    public object? Details { get; }
}