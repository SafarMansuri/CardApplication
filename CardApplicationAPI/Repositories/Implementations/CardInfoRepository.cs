using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace CardApplicationAPI.Repositories.Implementations
{
    public class CardInfoRepository : ICardInfoRepository
    {
        private readonly AppDbContext _context;

        public CardInfoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<CardInfo>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            bool? isSettled = null,
            bool? limitUsed = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var query = _context.CardInfo.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    x.PartyName.ToLower().Contains(term) ||
                    x.CardNumber.ToLower().Contains(term) ||
                    (x.BankName != null && x.BankName.ToLower().Contains(term)) ||
                    x.CardName.ToLower().Contains(term));
            }

            if (isSettled.HasValue)
                query = query.Where(x => x.IsSettled == isSettled.Value);

            if (limitUsed.HasValue)
                query = query.Where(x => x.limitUsed == limitUsed.Value);

            var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            query = ApplySort(query, sortBy, asc);

            var result = await query.ToPaginatedResultAsync(pagination);
            return new PaginatedResult<CardInfo> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        private static IQueryable<CardInfoEntity> ApplySort(IQueryable<CardInfoEntity> query, string? sortBy, bool asc)
        {
            return (sortBy?.ToLowerInvariant()) switch
            {
                "id" => asc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
                "date" => asc ? query.OrderBy(x => x.Date).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id),
                "partyname" => asc ? query.OrderBy(x => x.PartyName).ThenBy(x => x.Id) : query.OrderByDescending(x => x.PartyName).ThenByDescending(x => x.Id),
                "cardname" => asc ? query.OrderBy(x => x.CardName).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CardName).ThenByDescending(x => x.Id),
                "cardnumber" => asc ? query.OrderBy(x => x.CardNumber).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CardNumber).ThenByDescending(x => x.Id),
                "bankname" => asc ? query.OrderBy(x => x.BankName).ThenBy(x => x.Id) : query.OrderByDescending(x => x.BankName).ThenByDescending(x => x.Id),
                "deduction" => asc ? query.OrderBy(x => x.Deduction).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Deduction).ThenByDescending(x => x.Id),
                "profit" => asc ? query.OrderBy(x => x.Profit).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Profit).ThenByDescending(x => x.Id),
                "pos" => asc ? query.OrderBy(x => x.POS).ThenBy(x => x.Id) : query.OrderByDescending(x => x.POS).ThenByDescending(x => x.Id),
                "model" => asc ? query.OrderBy(x => x.Model).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Model).ThenByDescending(x => x.Id),
                "swipeperson" => asc ? query.OrderBy(x => x.SwipePerson).ThenBy(x => x.Id) : query.OrderByDescending(x => x.SwipePerson).ThenByDescending(x => x.Id),
                "merchant" => asc ? query.OrderBy(x => x.Merchant).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Merchant).ThenByDescending(x => x.Id),
                "limitused" => asc ? query.OrderBy(x => x.limitUsed).ThenBy(x => x.Id) : query.OrderByDescending(x => x.limitUsed).ThenByDescending(x => x.Id),
                "issettled" => asc ? query.OrderBy(x => x.IsSettled).ThenBy(x => x.Id) : query.OrderByDescending(x => x.IsSettled).ThenByDescending(x => x.Id),
                "settledat" => asc ? query.OrderBy(x => x.SettledAt).ThenBy(x => x.Id) : query.OrderByDescending(x => x.SettledAt).ThenByDescending(x => x.Id),
                "createdat" => asc ? query.OrderBy(x => x.Createdat).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Createdat).ThenByDescending(x => x.Id),
                "updatedat" => asc ? query.OrderBy(x => x.Updatedat).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Updatedat).ThenByDescending(x => x.Id),
                _ => asc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id)
            };
        }

        public async Task<CardInfo?> GetByIdAsync(int id)
        {
            var entity = await _context.CardInfo.FirstOrDefaultAsync(u => u.Id == id);
            return entity != null ? MapToModel(entity) : null;
        }

        public async Task<CardInfo> CreateAsync(CardInfo model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var entity = MapToEntity(model);
            entity.Createdat = DateTime.UtcNow;
            entity.Updatedat = DateTime.UtcNow;
            entity.PartyName = entity.PartyName!.Trim();

            _context.CardInfo.Add(entity);
            await _context.SaveChangesAsync();
            await SyncPaymentInfoForPartyAsync(entity.PartyName);
            await transaction.CommitAsync();

            return MapToModel(entity);
        }

        public async Task<CardInfo> UpdateAsync(CardInfo model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var existing = await _context.CardInfo.FirstOrDefaultAsync(u => u.Id == model.Id);
            if (existing == null)
                throw new KeyNotFoundException($"CardInfo with ID {model.Id} not found");

            if (existing.IsSettled)
                throw new InvalidOperationException("Settled card info cannot be updated.");

            var oldPartyName = existing.PartyName;

            existing.CardName = model.CardName;
            existing.CardNumber = model.CardNumber;
            existing.IMEI = model.IMEI;
            existing.PartyName = model.PartyName;
            existing.Deduction = model.Deduction;
            existing.Profit = model.Profit;
            existing.Date = model.Date;
            existing.AdditionalInfo = model.AdditionalInfo;
            existing.POS = model.POS;
            existing.Remarks = model.Remarks;
            existing.limitUsed = model.limitUsed;
            existing.BankName = model.BankName;
            existing.Model = model.Model;
            existing.Merchant = model.Merchant;
            existing.SwipePerson = model.SwipePerson;
            existing.PartyName = model.PartyName!.Trim();
            existing.Updatedat = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await SyncPaymentInfoForPartyAsync(oldPartyName);
            if (!string.Equals(oldPartyName, existing.PartyName, StringComparison.OrdinalIgnoreCase))
                await SyncPaymentInfoForPartyAsync(existing.PartyName);
            await transaction.CommitAsync();

            return MapToModel(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var entity = await _context.CardInfo.FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null)
            {
                await transaction.RollbackAsync();
                return false;
            }
            if (entity.IsSettled)
                throw new InvalidOperationException("Settled card info cannot be deleted.");

            var partyName = entity.PartyName;
            _context.CardInfo.Remove(entity);
            await _context.SaveChangesAsync();
            await SyncPaymentInfoForPartyAsync(partyName);
            await transaction.CommitAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.CardInfo.AnyAsync(u => u.Id == id);
        }


        private static CardInfo MapToModel(CardInfoEntity entity)
        {
            return new CardInfo
            {
                Id = entity.Id,
                CardName = entity.CardName,
                CardNumber = entity.CardNumber,
                IMEI = entity.IMEI,
                PartyName = entity.PartyName,
                Deduction = entity.Deduction,
                Profit = entity.Profit,
                Date = entity.Date,
                AdditionalInfo = entity.AdditionalInfo,
                POS = entity.POS,
                Remarks = entity.Remarks,
                limitUsed = entity.limitUsed,
                BankName = entity.BankName,
                Model = entity.Model,
                Merchant = entity.Merchant,
                SwipePerson = entity.SwipePerson,
                IsSettled = entity.IsSettled,
                SettledAt = entity.SettledAt,
                Createdat = entity.Createdat,
                Updatedat = entity.Updatedat
            };
        }

        private static CardInfoEntity MapToEntity(CardInfo model)
        {
            return new CardInfoEntity
            {
                Id = model.Id,
                CardName = model.CardName,
                CardNumber = model.CardNumber,
                IMEI = model.IMEI,
                PartyName = model.PartyName,
                Deduction = model.Deduction,
                Profit = model.Profit,
                Date = model.Date,
                AdditionalInfo = model.AdditionalInfo,
                POS = model.POS,
                Remarks = model.Remarks,
                limitUsed = model.limitUsed,
                BankName = model.BankName,
                Model = model.Model,
                Merchant = model.Merchant,
                SwipePerson = model.SwipePerson,
                IsSettled = false,
                SettledAt = null,
                Createdat = model.Createdat,
                Updatedat = model.Updatedat
            };

        }

        private async Task SyncPaymentInfoForPartyAsync(string? partyName)
        {
            if (string.IsNullOrWhiteSpace(partyName))
                return;

            var entries = await _context.CardInfo
                .AsNoTracking()
                .Where(x => x.PartyName == partyName)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var existingSummary = await _context.PaymentInfo
                .FirstOrDefaultAsync(x => x.PartyName == partyName);

            if (entries.Count == 0)
            {
                if (existingSummary != null)
                    _context.PaymentInfo.Remove(existingSummary);
                await _context.SaveChangesAsync();
                return;
            }

            var latest = entries[0];
            var swipeCount = entries.Count;
            var totalAmount = entries.Sum(x => x.Deduction ?? 0m);
            var totalProfit = entries.Sum(x => x.Profit ?? 0m);
            var pendingAmount = entries.Where(x => !x.IsSettled).Sum(x => x.Deduction ?? 0m);
            var summary = existingSummary ?? new PaymentInfoEntity();
            summary.PartyName = latest.PartyName!;
            summary.Date = latest.Date;
            summary.SwipeCount = swipeCount;
            summary.SwipeAmount = latest.Deduction;
            summary.TotalAmount = totalAmount;
            summary.Profit = latest.Profit;
            summary.TotalProfit = totalProfit;
            summary.PaidAmount = totalAmount - pendingAmount;
            summary.PendingAmount = pendingAmount;
            summary.PaymentStatus = pendingAmount == 0m ? "Paid" : "Pending";
            summary.LimitUsed = latest.limitUsed;
            summary.CardName = latest.CardName;
            summary.Merchant = latest.Merchant;
            summary.SwipePerson = latest.SwipePerson;
            summary.Remarks = latest.Remarks;
            summary.Updatedat = DateTime.UtcNow;

            if (existingSummary == null)
            {
                summary.Createdat = DateTime.UtcNow;
                _context.PaymentInfo.Add(summary);
            }

            await _context.SaveChangesAsync();
        }
    }
}
