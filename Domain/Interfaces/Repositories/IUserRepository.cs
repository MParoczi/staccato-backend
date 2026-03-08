using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);

    /// <summary>
    /// Returns the user and their currently active (non-revoked, non-expired) refresh tokens.
    /// Returns null if no user with the given ID exists.
    /// </summary>
    Task<(User User, IReadOnlyList<RefreshToken> Tokens)?> GetWithActiveTokensAsync(
        Guid userId, CancellationToken ct = default);
}
