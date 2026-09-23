using System.Security.Cryptography;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models.Settings;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CardApplicationAPI.Services.Implementations
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly JwtSettings _settings;

        public RefreshTokenService(
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            IJwtTokenService jwtTokenService,
            IOptions<JwtSettings> options)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _settings = options.Value;
        }

        public async Task<RefreshTokenEntity> GenerateRefreshTokenAsync(int userId)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var entity = new RefreshTokenEntity
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshExpiryDays)
            };

            return await _refreshTokenRepository.CreateAsync(entity);
        }

        public async Task<(string? accessToken, string? refreshToken, string? error)> RotateAsync(string refreshToken)
        {
            var stored = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (stored == null)
                return (null, null, "Invalid refresh token.");

            if (stored.IsRevoked)
                return (null, null, "Refresh token has been revoked.");

            if (stored.ExpiresAt < DateTime.UtcNow)
                return (null, null, "Refresh token has expired.");

            var user = await _userRepository.GetByIdAsync(stored.UserId);
            if (user == null)
                return (null, null, "User not found.");

            var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);
            await _refreshTokenRepository.RevokeAsync(stored, newRefreshToken.Token);

            var accessToken = _jwtTokenService.GenerateAccessToken(user);

            return (accessToken, newRefreshToken.Token, null);
        }

        public async Task<bool> RevokeAsync(string refreshToken)
        {
            var stored = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
                return false;

            await _refreshTokenRepository.RevokeAsync(stored);
            return true;
        }
    }
}
