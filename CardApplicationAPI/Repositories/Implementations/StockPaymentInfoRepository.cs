using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class StockPaymentInfoRepository : IStockPaymentInfoRepository
    {
        private readonly AppDbContext _context;

        public StockPaymentInfoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<StockPaymentInfo>> GetAllAsync(PaginationParams pagination, string? partyName, string? inOut, string? paymentType, string? status)
        {
            var query = _context.StockPaymentInfo.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(partyName))
                query = query.Where(x => x.PartyName != null && x.PartyName.ToLower() == partyName.Trim().ToLower());
            if (!string.IsNullOrWhiteSpace(inOut))
                query = query.Where(x => x.InOut == inOut);
            if (!string.IsNullOrWhiteSpace(paymentType))
                query = query.Where(x => x.PaymentType == paymentType);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.PaymentStatus == status);
            var result = await query.OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockPaymentInfo> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<StockPaymentInfo?> GetByIdAsync(int id)
        {
            var entity = await _context.StockPaymentInfo.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return entity == null ? null : MapToModel(entity);
        }

        public async Task<PaginatedResult<StockPaymentInfo>> GetByStockItemIdAsync(int stockItemId, PaginationParams pagination)
        {
            var result = await _context.StockPaymentInfo.AsNoTracking().Where(x => x.StockItemId == stockItemId)
                .OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockPaymentInfo> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<StockPaymentInfo?> SettleAsync(int id, StockPaymentSettlementRequest request, int userId, string username)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var entity = await _context.StockPaymentInfo
                .FromSqlInterpolated($"SELECT * FROM [StockPaymentInfo] WITH (UPDLOCK) WHERE [Id] = {id}")
                .FirstOrDefaultAsync();
            if (entity == null) return null;
            if (entity.PaymentStatus == "Completed")
                throw new InvalidOperationException("Stock payment is already completed.");
            if (request.Amount != entity.OutstandingAmount)
                throw new ArgumentException($"Settlement amount must equal the outstanding amount of {entity.OutstandingAmount:0.00}.");

            var completedAt = DateTime.UtcNow;
            _context.StockPaymentHistory.Add(new StockPaymentHistoryEntity
            {
                StockPaymentInfoId = entity.Id,
                EventType = "Settlement",
                Amount = request.Amount,
                TransactionDate = request.TransactionDate,
                PaymentMethod = request.PaymentMethod.Trim(),
                ReferenceNo = request.ReferenceNo?.Trim(),
                Remarks = request.Remarks?.Trim(),
                PerformedByUserId = userId,
                PerformedByUsername = username,
                CreatedAt = completedAt
            });
            entity.PaymentStatus = "Completed";
            entity.OutstandingAmount = 0m;
            entity.CompletedAt = completedAt;
            entity.CompletedByUserId = userId;
            entity.CompletedByUsername = username;
            entity.UpdatedAt = completedAt;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return MapToModel(entity);
        }

        public async Task<StockPaymentInfo?> ResetAsync(int id, StockPaymentResetRequest request, int userId, string username)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var entity = await _context.StockPaymentInfo
                .FromSqlInterpolated($"SELECT * FROM [StockPaymentInfo] WITH (UPDLOCK) WHERE [Id] = {id}")
                .FirstOrDefaultAsync();
            if (entity == null) return null;
            await _context.Entry(entity).Reference(x => x.StockItem).LoadAsync();
            if (entity.PaymentStatus == "Pending")
                throw new InvalidOperationException("Stock payment is already pending.");

            var resetAt = DateTime.UtcNow;
            _context.StockPaymentHistory.Add(new StockPaymentHistoryEntity
            {
                StockPaymentInfoId = entity.Id,
                EventType = "Reset",
                Amount = entity.Amount ?? 0m,
                TransactionDate = resetAt,
                Remarks = request.Reason.Trim(),
                PerformedByUserId = userId,
                PerformedByUsername = username,
                CreatedAt = resetAt
            });
            entity.PaymentStatus = "Pending";
            entity.OutstandingAmount = entity.StockItem?.Amount ?? entity.Amount ?? 0m;
            entity.CompletedAt = null;
            entity.CompletedByUserId = null;
            entity.CompletedByUsername = null;
            entity.UpdatedAt = resetAt;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return MapToModel(entity);
        }

        public async Task<PaginatedResult<StockPaymentHistory>> GetHistoryAsync(int id, PaginationParams pagination)
        {
            var result = await _context.StockPaymentHistory.AsNoTracking().Where(x => x.StockPaymentInfoId == id)
                .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockPaymentHistory>
            {
                Items = result.Items.Select(x => new StockPaymentHistory
                {
                    Id = x.Id, StockPaymentInfoId = x.StockPaymentInfoId, EventType = x.EventType, Amount = x.Amount,
                    TransactionDate = x.TransactionDate, PaymentMethod = x.PaymentMethod, ReferenceNo = x.ReferenceNo,
                    Remarks = x.Remarks, PerformedByUserId = x.PerformedByUserId,
                    PerformedByUsername = x.PerformedByUsername, CreatedAt = x.CreatedAt
                }),
                Pagination = result.Pagination
            };
        }

        public async Task<PaginatedResult<StockPartyPaymentSummary>> GetPartySummariesAsync(PaginationParams pagination, string? partyName, string? paymentType, string? status)
        {
            var query = _context.StockPaymentInfo.AsNoTracking().Where(x => x.PartyName != null);
            if (!string.IsNullOrWhiteSpace(partyName)) query = query.Where(x => x.PartyName == partyName.Trim());
            if (!string.IsNullOrWhiteSpace(paymentType)) query = query.Where(x => x.PaymentType == paymentType);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.PaymentStatus == status);
            return await query.GroupBy(x => new { x.PartyName, x.PaymentType })
                .Select(group => new StockPartyPaymentSummary
                {
                    PartyName = group.Key.PartyName!, PaymentType = group.Key.PaymentType, ItemCount = group.Count(),
                    TotalAmount = group.Sum(x => x.Amount ?? 0m),
                    CompletedAmount = group.Sum(x => (x.Amount ?? 0m) - x.OutstandingAmount),
                    OutstandingAmount = group.Sum(x => x.OutstandingAmount)
                }).OrderBy(x => x.PartyName).ThenBy(x => x.PaymentType).ToPaginatedResultAsync(pagination);
        }

        public async Task<StockPaymentTotals> GetTotalsAsync()
        {
            var payable = await _context.StockPaymentInfo.Where(x => x.PaymentType == "Payable").SumAsync(x => (decimal?)x.OutstandingAmount) ?? 0m;
            var receivable = await _context.StockPaymentInfo.Where(x => x.PaymentType == "Receivable").SumAsync(x => (decimal?)x.OutstandingAmount) ?? 0m;
            return new StockPaymentTotals(payable, receivable);
        }

        private static StockPaymentInfo MapToModel(StockPaymentInfoEntity entity) => new()
        {
            Id = entity.Id, No = entity.No, AccountName = entity.AccountName, AccountNo = entity.AccountNo,
            Ifsc = entity.Ifsc, Amount = entity.Amount, Date = entity.Date, PartyName = entity.PartyName,
            Bank = entity.Bank, InOut = entity.InOut, PaymentType = entity.PaymentType,
            PaymentStatus = entity.PaymentStatus, OutstandingAmount = entity.OutstandingAmount,
            CompletedAt = entity.CompletedAt, CompletedByUserId = entity.CompletedByUserId,
            CompletedByUsername = entity.CompletedByUsername, Remarks = entity.Remarks,
            StockItemId = entity.StockItemId, CreatedAt = entity.CreatedAt, UpdatedAt = entity.UpdatedAt
        };
    }
}
