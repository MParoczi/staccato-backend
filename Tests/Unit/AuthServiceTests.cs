using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Constants;
using DomainModels.Models;
using Moq;

namespace Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IGoogleTokenValidator> _googleValidator = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _userRepo = new();

    public AuthServiceTests()
    {
        _jwt.Setup(j => j.AccessTokenExpirySeconds).Returns(900);
        _jwt.Setup(j => j.RefreshTokenExpiryDays).Returns(7);
        _jwt.Setup(j => j.RememberMeExpiryDays).Returns(30);
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _refreshTokenRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private AuthService CreateService()
    {
        return new AuthService(_userRepo.Object, _refreshTokenRepo.Object, _uow.Object,
            _hasher.Object, _jwt.Object, _googleValidator.Object);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsAuthTokens()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateService().RegisterAsync("test@example.com", "Jane Doe", "password123");

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal(900, result.ExpiresIn);
        Assert.Equal("refresh-token", result.RefreshToken);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflict()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "test@example.com" });

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().RegisterAsync("test@example.com", "Jane Doe", "password123"));

        Assert.Equal(AuthErrorCodes.EmailAlreadyRegistered, ex.Code);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthTokens()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "hashed" };
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("password", "hashed")).Returns(true);

        var result = await CreateService().LoginAsync("test@example.com", "password");

        Assert.Equal("access-token", result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "hashed" };
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().LoginAsync("test@example.com", "wrong"));

        Assert.Equal(AuthErrorCodes.InvalidCredentials, ex.Code);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().LoginAsync("unknown@example.com", "password"));

        Assert.Equal(AuthErrorCodes.InvalidCredentials, ex.Code);
    }

    [Fact]
    public async Task LoginAsync_NoPasswordSet_ThrowsUnauthorized()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", PasswordHash = null };
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().LoginAsync("test@example.com", "password"));

        Assert.Equal(AuthErrorCodes.NoPasswordSet, ex.Code);
    }

    [Fact]
    public async Task LoginAsync_RememberMe_SetsLongerExpiry()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "hashed" };
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("password", "hashed")).Returns(true);

        RefreshToken? captured = null;
        _refreshTokenRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        await CreateService().LoginAsync("test@example.com", "password", true);

        Assert.NotNull(captured);
        Assert.True((captured!.ExpiresAt - DateTime.UtcNow).TotalDays > 25,
            "RememberMe should produce ~30-day expiry");
    }

    // ── GoogleLoginAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GoogleLoginAsync_NewUser_CreatesAccountAndReturnsTokens()
    {
        var info = new GoogleUserInfo("gid-1", "new@example.com", "New User", null);
        _googleValidator.Setup(v => v.ValidateAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(info);
        _userRepo.Setup(r => r.GetByGoogleIdAsync("gid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateService().GoogleLoginAsync("token");

        Assert.Equal("access-token", result.AccessToken);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoogleLoginAsync_ExistingEmail_LinksGoogleToAccount()
    {
        var existing = new User { Id = Guid.NewGuid(), Email = "ex@example.com", PasswordHash = "hashed" };
        var info = new GoogleUserInfo("gid-1", "ex@example.com", "Existing User", "https://pic.example.com");
        _googleValidator.Setup(v => v.ValidateAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(info);
        _userRepo.Setup(r => r.GetByGoogleIdAsync("gid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByEmailAsync("ex@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateService().GoogleLoginAsync("token");

        Assert.Equal("gid-1", existing.GoogleId);
        Assert.Equal("https://pic.example.com", existing.AvatarUrl);
        _userRepo.Verify(r => r.Update(existing), Times.Once);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GoogleLoginAsync_ExistingGoogleId_LogsIn()
    {
        var existing = new User { Id = Guid.NewGuid(), Email = "test@example.com", GoogleId = "gid-1" };
        var info = new GoogleUserInfo("gid-1", "test@example.com", "Test User", null);
        _googleValidator.Setup(v => v.ValidateAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(info);
        _userRepo.Setup(r => r.GetByGoogleIdAsync("gid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateService().GoogleLoginAsync("token");

        Assert.Equal("access-token", result.AccessToken);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GoogleLoginAsync_InvalidToken_ThrowsUnauthorized()
    {
        _googleValidator.Setup(v => v.ValidateAsync("bad", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException(AuthErrorCodes.GoogleAuthFailed, "Google Sign-In failed."));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().GoogleLoginAsync("bad"));

        Assert.Equal(AuthErrorCodes.GoogleAuthFailed, ex.Code);
    }

    [Fact]
    public async Task GoogleLoginAsync_ServiceDown_ThrowsServiceUnavailable()
    {
        _googleValidator.Setup(v => v.ValidateAsync("token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException());

        await Assert.ThrowsAsync<ServiceUnavailableException>(() => CreateService().GoogleLoginAsync("token"));
    }

    // ── RefreshAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesAndReturnsNew()
    {
        var userId = Guid.NewGuid();
        var originalExpiry = DateTime.UtcNow.AddDays(7);
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(), Token = "old", UserId = userId,
            ExpiresAt = originalExpiry, IsRevoked = false
        };
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, Email = "test@example.com" });

        var result = await CreateService().RefreshAsync("old");

        Assert.True(token.IsRevoked, "Old token should be marked revoked");
        Assert.Equal(originalExpiry, result.RefreshTokenExpiry);
        _refreshTokenRepo.Verify(r => r.Update(token), Times.Once);
        _refreshTokenRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_RevokesAllAndThrows()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(), Token = "revoked", UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = true
        };
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("revoked", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _refreshTokenRepo.Setup(r => r.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().RefreshAsync("revoked"));

        Assert.Equal(AuthErrorCodes.InvalidToken, ex.Code);
        _refreshTokenRepo.Verify(r => r.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ThrowsUnauthorized()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(), Token = "expired", UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1), IsRevoked = false
        };
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().RefreshAsync("expired"));

        Assert.Equal(AuthErrorCodes.TokenExpired, ex.Code);
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_ThrowsUnauthorized()
    {
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().RefreshAsync("unknown"));

        Assert.Equal(AuthErrorCodes.InvalidToken, ex.Code);
    }

    // ── LogoutAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesToken()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(), Token = "valid", UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = false
        };
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("valid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        await CreateService().LogoutAsync("valid");

        Assert.True(token.IsRevoked);
        _refreshTokenRepo.Verify(r => r.Update(token), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_UnknownToken_IsIdempotent()
    {
        _refreshTokenRepo.Setup(r => r.GetByTokenAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await CreateService().LogoutAsync("unknown"); // must not throw

        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}