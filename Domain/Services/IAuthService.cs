using DomainModels.Models;

namespace Domain.Services;

public interface IAuthService
{
    Task<AuthTokens> RegisterAsync(string email, string displayName, string password,
        CancellationToken ct = default);

    Task<AuthTokens> LoginAsync(string email, string password, bool rememberMe = false,
        CancellationToken ct = default);

    Task<AuthTokens> GoogleLoginAsync(string idToken, CancellationToken ct = default);

    Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);

    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}
