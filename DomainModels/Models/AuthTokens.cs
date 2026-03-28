namespace DomainModels.Models;

public sealed record AuthTokens(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    DateTime RefreshTokenExpiry);