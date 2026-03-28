using System.Text.Json;
using Application.Resources;
using Domain.Exceptions;
using Microsoft.Extensions.Localization;

namespace Application.Middleware;

public sealed class BusinessExceptionMiddleware(
    RequestDelegate next,
    IStringLocalizer<BusinessErrors> localizer)
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

            var localizedString = localizer[ex.Code];
            var message = localizedString.ResourceNotFound ? ex.Message : localizedString.Value;

            var payload = new { code = ex.Code, message, details = ex.Details };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}