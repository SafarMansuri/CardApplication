using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshTokenEntity?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken)
        {
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task RevokeAsync(RefreshTokenEntity refreshToken, string? replacedByToken = null)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.ReplacedByToken = replacedByToken;
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> TryRotateAsync(RefreshTokenEntity current, RefreshTokenEntity replacement)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;
            var changed = await _context.RefreshTokens
                .Where(x => x.Id == current.Id && x.RevokedAt == null && x.ExpiresAt > now)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.RevokedAt, (DateTime?)now)
                    .SetProperty(x => x.ReplacedByToken, replacement.Token));
            if (changed != 1)
                return false;

            _context.RefreshTokens.Add(replacement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task RemoveExpiredAsync()
        {
            var expired = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expired.Any())
            {
                _context.RefreshTokens.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
        }
    }
}
