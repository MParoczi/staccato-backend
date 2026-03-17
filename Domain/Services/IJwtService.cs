using DomainModels.Models;

namespace Domain.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int AccessTokenExpirySeconds { get; }
    int RefreshTokenExpiryDays   { get; }
    int RememberMeExpiryDays     { get; }
}
