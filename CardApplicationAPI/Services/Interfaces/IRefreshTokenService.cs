using CardApplicationAPI.Data.Entities;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshTokenEntity> GenerateRefreshTokenAsync(int userId);
        Task<(string? accessToken, string? refreshToken, string? error)> RotateAsync(string refreshToken);
        Task<bool> RevokeAsync(string refreshToken);
    }
}
