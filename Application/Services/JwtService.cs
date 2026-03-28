using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Options;
using Domain.Services;
using DomainModels.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services;

public sealed class JwtService(IOptions<JwtOptions> options) : IJwtService
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("displayName", $"{user.FirstName} {user.LastName}".Trim())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes),
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public int AccessTokenExpirySeconds => _options.AccessTokenExpiryMinutes * 60;
    public int RefreshTokenExpiryDays => _options.RefreshTokenExpiryDays;
    public int RememberMeExpiryDays => _options.RememberMeExpiryDays;
}