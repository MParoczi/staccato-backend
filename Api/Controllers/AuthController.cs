using ApiModels.Auth;
using Domain.Exceptions;
using Domain.Services;
using DomainModels.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request.Email, request.DisplayName, request.Password, ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return StatusCode(StatusCodes.Status201Created, new AuthResponse(result.AccessToken, result.ExpiresIn));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, request.RememberMe, ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return Ok(new AuthResponse(result.AccessToken, result.ExpiresIn));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var tokenValue = Request.Cookies["staccato_refresh"];
        if (string.IsNullOrWhiteSpace(tokenValue))
            throw new UnauthorizedException(AuthErrorCodes.InvalidToken, "No refresh token provided.");

        var result = await authService.RefreshAsync(tokenValue, ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return Ok(new AuthResponse(result.AccessToken, result.ExpiresIn));
    }

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
