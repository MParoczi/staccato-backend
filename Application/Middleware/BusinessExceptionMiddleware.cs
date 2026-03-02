using System.Text.Json;
using Domain.Exceptions;

namespace Application.Middleware;

public sealed class BusinessExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BusinessException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            var payload = new { code = ex.Code, message = ex.Message, details = ex.Details };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}