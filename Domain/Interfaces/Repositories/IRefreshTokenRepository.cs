using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    ///     Returns all non-revoked, non-expired tokens for the user.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Bulk-revokes all refresh tokens for the user via a single SQL UPDATE.
    ///     NOTE: This call commits immediately and does NOT participate in IUnitOfWork.
    ///     Callers MUST NOT call IUnitOfWork.CommitAsync for this operation — the revocation
    ///     is already persisted when this method returns.
    ///     Use for logout-all-devices and account deletion flows only.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}