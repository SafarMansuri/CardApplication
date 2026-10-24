using CardApplicationAPI.Data.Entities;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshTokenEntity?> GetByTokenAsync(string token);
        Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken);
        Task<bool> TryRotateAsync(RefreshTokenEntity current, RefreshTokenEntity replacement);
        Task RevokeAsync(RefreshTokenEntity refreshToken, string? replacedByToken = null);
        Task RemoveExpiredAsync();
    }
}
