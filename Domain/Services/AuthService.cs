using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Models;

namespace Domain.Services;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IAuthService
{
    public Task<AuthTokens> RegisterAsync(string email, string displayName, string password,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<AuthTokens> LoginAsync(string email, string password, bool rememberMe = false,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<AuthTokens> GoogleLoginAsync(string idToken, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task LogoutAsync(string refreshToken, CancellationToken ct = default) =>
        throw new NotImplementedException();
}
