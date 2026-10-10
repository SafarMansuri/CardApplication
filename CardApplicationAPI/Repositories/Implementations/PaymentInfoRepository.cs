using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class PaymentInfoRepository : IPaymentInfoRepository
    {
        private readonly AppDbContext _context;

        public PaymentInfoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<PaymentInfo>> GetAllAsync(PaginationParams pagination, string? partyName, string? status)
        {
            var query = _context.PaymentInfo.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(partyName))
            {
                var normalizedPartyName = partyName.Trim();
                query = query.Where(x => x.PartyName == normalizedPartyName);
            }
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.PaymentStatus == status);

            var result = await query.OrderBy(x => x.PartyName).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<PaymentInfo>
            {
                Items = result.Items.Select(MapToModel),
                Pagination = result.Pagination
            };
        }

        public async Task<PaymentInfo?> MarkPaidAsync(int id, int paidByUserId, string paidByUsername)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var summary = await _context.PaymentInfo.FirstOrDefaultAsync(x => x.Id == id);
            if (summary == null)
                return null;

            var pendingEntries = await _context.CardInfo
                .Where(x => x.PartyName == summary.PartyName && !x.IsSettled)
                .ToListAsync();
            if (pendingEntries.Count == 0)
                throw new InvalidOperationException("This party has no pending amount to pay.");

            var paidAt = DateTime.UtcNow;
            foreach (var entry in pendingEntries)
            {
                entry.IsSettled = true;
                entry.SettledAt = paidAt;
                entry.Updatedat = paidAt;
            }

            var totalAmount = await _context.CardInfo
                .Where(x => x.PartyName == summary.PartyName)
                .SumAsync(x => x.Deduction ?? 0m);
            summary.TotalAmount = totalAmount;
            summary.PaidAmount = totalAmount;
            summary.PendingAmount = 0m;
            summary.PaymentStatus = "Paid";
            summary.LastPaidAt = paidAt;
            summary.LastPaidByUserId = paidByUserId;
            summary.LastPaidByUsername = paidByUsername;
            summary.Updatedat = paidAt;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return MapToModel(summary);
        }

        private static PaymentInfo MapToModel(CardApplicationAPI.Data.Entities.PaymentInfoEntity entity)
        {
            return new PaymentInfo
            {
                Id = entity.Id,
                Date = entity.Date,
                PartyName = entity.PartyName,
                SwipeCount = entity.SwipeCount,
                SwipeAmount = entity.SwipeAmount,
                TotalAmount = entity.TotalAmount,
                Profit = entity.Profit,
                TotalProfit = entity.TotalProfit,
                PaidAmount = entity.PaidAmount,
                PendingAmount = entity.PendingAmount,
                PaymentStatus = entity.PaymentStatus,
                LastPaidAt = entity.LastPaidAt,
                LastPaidByUserId = entity.LastPaidByUserId,
                LastPaidByUsername = entity.LastPaidByUsername,
                LimitUsed = entity.LimitUsed,
                CardName = entity.CardName,
                Merchant = entity.Merchant,
                SwipePerson = entity.SwipePerson,
                Remarks = entity.Remarks,
                Createdat = entity.Createdat,
                Updatedat = entity.Updatedat
            };
        }
    }
}
