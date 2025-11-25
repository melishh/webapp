using SGE.Core.Entities;

namespace SGE.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>
    /// Retrieves a refresh token by its token string.
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// Retrieves all active refresh tokens for a specific user.
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(string userId);

    /// <summary>
    /// Revokes all active refresh tokens for a specific user, with a reason.
    /// </summary>
    Task RevokeAllUserTokensAsync(string userId, string reason);
}
