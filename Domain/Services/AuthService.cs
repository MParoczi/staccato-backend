using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Constants;
using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IAuthService
{
    public async Task<AuthTokens> RegisterAsync(string email, string displayName, string password,
        CancellationToken ct = default)
    {
        var existing = await userRepository.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new ConflictException(AuthErrorCodes.EmailAlreadyRegistered,
                "An account with this email address already exists.");

        var parts = displayName.Split(' ', 2);
        var firstName = parts[0];
        var lastName  = parts.Length > 1 ? parts[1] : string.Empty;

        var passwordHash = passwordHasher.Hash(password);

        var user = new User
        {
            Id           = Guid.NewGuid(),
            Email        = email,
            FirstName    = firstName,
            LastName     = lastName,
            PasswordHash = passwordHash,
            CreatedAt    = DateTime.UtcNow,
            Language     = Language.English
        };

        await userRepository.AddAsync(user, ct);

        var tokenValue = jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id        = Guid.NewGuid(),
            Token     = tokenValue,
            UserId    = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtService.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await refreshTokenRepository.AddAsync(refreshToken, ct);
        await uow.CommitAsync(ct);

        return new AuthTokens(
            jwtService.GenerateAccessToken(user),
            jwtService.AccessTokenExpirySeconds,
            tokenValue,
            refreshToken.ExpiresAt);
    }

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
