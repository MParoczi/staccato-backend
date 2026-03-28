using DomainModels.Models;

namespace Domain.Services;

public interface IJwtService
{
    int AccessTokenExpirySeconds { get; }
    int RefreshTokenExpiryDays { get; }
    int RememberMeExpiryDays { get; }
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}