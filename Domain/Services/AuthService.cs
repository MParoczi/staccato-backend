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
    IJwtService jwtService,
    IGoogleTokenValidator googleTokenValidator) : IAuthService
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

    public async Task<AuthTokens> LoginAsync(string email, string password, bool rememberMe = false,
        CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(email, ct);
        if (user is null)
            throw new UnauthorizedException(AuthErrorCodes.InvalidCredentials,
                "Invalid email address or password.");

        if (user.PasswordHash is null)
            throw new UnauthorizedException(AuthErrorCodes.NoPasswordSet,
                "This account uses Google Sign-In. Please log in with Google.");

        if (!passwordHasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedException(AuthErrorCodes.InvalidCredentials,
                "Invalid email address or password.");

        var expiryDays  = rememberMe ? jwtService.RememberMeExpiryDays : jwtService.RefreshTokenExpiryDays;
        var tokenValue  = jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id        = Guid.NewGuid(),
            Token     = tokenValue,
            UserId    = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
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

    public async Task<AuthTokens> GoogleLoginAsync(string idToken, CancellationToken ct = default)
    {
        var googleUserInfo = await googleTokenValidator.ValidateAsync(idToken, ct);

        var user = await userRepository.GetByGoogleIdAsync(googleUserInfo.GoogleId, ct);

        if (user is null)
        {
            user = await userRepository.GetByEmailAsync(googleUserInfo.Email, ct);
            if (user is not null)
            {
                user.GoogleId = googleUserInfo.GoogleId;
                if (user.AvatarUrl is null)
                    user.AvatarUrl = googleUserInfo.PictureUrl;
                userRepository.Update(user);
            }
        }

        if (user is null)
        {
            var parts     = googleUserInfo.Name?.Split(' ', 2) ?? [];
            var firstName = parts.Length > 0 ? parts[0] : string.Empty;
            var lastName  = parts.Length > 1 ? parts[1] : string.Empty;

            user = new User
            {
                Id        = Guid.NewGuid(),
                Email     = googleUserInfo.Email,
                FirstName = firstName,
                LastName  = lastName,
                GoogleId  = googleUserInfo.GoogleId,
                AvatarUrl = googleUserInfo.PictureUrl,
                CreatedAt = DateTime.UtcNow,
                Language  = Language.English
            };

            await userRepository.AddAsync(user, ct);
        }

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

    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await refreshTokenRepository.GetByTokenAsync(refreshToken, ct);
        if (token is null)
            throw new UnauthorizedException(AuthErrorCodes.InvalidToken,
                "The refresh token is invalid.");

        if (token.IsRevoked)
        {
            await refreshTokenRepository.RevokeAllForUserAsync(token.UserId, ct);
            throw new UnauthorizedException(AuthErrorCodes.InvalidToken,
                "The refresh token is invalid.");
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedException(AuthErrorCodes.TokenExpired,
                "The refresh token has expired. Please log in again.");

        var user = await userRepository.GetByIdAsync(token.UserId, ct);
        if (user is null)
            throw new NotFoundException("User not found.");

        token.IsRevoked = true;
        refreshTokenRepository.Update(token);

        var newTokenValue = jwtService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id        = Guid.NewGuid(),
            Token     = newTokenValue,
            UserId    = user.Id,
            ExpiresAt = token.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await refreshTokenRepository.AddAsync(newRefreshToken, ct);
        await uow.CommitAsync(ct);

        return new AuthTokens(
            jwtService.GenerateAccessToken(user),
            jwtService.AccessTokenExpirySeconds,
            newTokenValue,
            token.ExpiresAt);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await refreshTokenRepository.GetByTokenAsync(refreshToken, ct);
        if (token is null)
            return;

        token.IsRevoked = true;
        refreshTokenRepository.Update(token);
        await uow.CommitAsync(ct);
    }
}
