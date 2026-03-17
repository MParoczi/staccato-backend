using Domain.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService, IWebHostEnvironment env) : ControllerBase
{
    private void SetRefreshCookie(string token, DateTime expiry)
    {
        Response.Cookies.Append("staccato_refresh", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure   = !env.IsDevelopment(),
            Expires  = expiry
        });
    }

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete("staccato_refresh");
}
