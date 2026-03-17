namespace Domain.Exceptions;

public class ServiceUnavailableException : BusinessException
{
    public ServiceUnavailableException(string message, object? details = null)
        : base("SERVICE_UNAVAILABLE", message, details)
    {
        StatusCode = 503;
    }

    public ServiceUnavailableException()
        : this("An external service is temporarily unavailable. Please try again later.")
    {
    }
}
