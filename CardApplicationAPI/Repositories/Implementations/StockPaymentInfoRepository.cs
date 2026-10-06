using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class StockPaymentInfoRepository : IStockPaymentInfoRepository
    {
        private readonly AppDbContext _context;

        public StockPaymentInfoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<StockPaymentInfo>> GetAllAsync(PaginationParams pagination, string? partyName = null, string? inOut = null)
        {
            var query = _context.StockPaymentInfo
                .AsNoTracking()
                .Include(x => x.StockItem)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(partyName))
                query = query.Where(x => x.PartyName != null && x.PartyName.ToLower() == partyName.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(inOut))
                query = query.Where(x => x.InOut != null && x.InOut.ToLower() == inOut.Trim().ToLower());

            var result = await query.OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockPaymentInfo> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<StockPaymentInfo?> GetByIdAsync(int id)
        {
            var entity = await _context.StockPaymentInfo
                .Include(x => x.StockItem)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity == null ? null : MapToModel(entity);
        }

        public async Task<PaginatedResult<StockPaymentInfo>> GetByStockItemIdAsync(int stockItemId, PaginationParams pagination)
        {
            var result = await _context.StockPaymentInfo.Where(x => x.StockItemId == stockItemId)
                .OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockPaymentInfo> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<StockPaymentInfo> CreateAsync(StockPaymentInfo model)
        {
            var entity = MapToEntity(model);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.StockPaymentInfo.Add(entity);
            await _context.SaveChangesAsync();

            return MapToModel(entity);
        }

        public async Task<StockPaymentInfo> UpdateAsync(StockPaymentInfo model)
        {
            var existing = await _context.StockPaymentInfo.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing == null)
                throw new InvalidOperationException($"Stock payment with ID {model.Id} not found");

            existing.No = model.No;
            existing.AccountName = model.AccountName;
            existing.AccountNo = model.AccountNo;
            existing.Ifsc = model.Ifsc;
            existing.Amount = model.Amount;
            existing.Date = model.Date;
            existing.PartyName = model.PartyName;
            existing.Bank = model.Bank;
            existing.InOut = model.InOut;
            existing.Remarks = model.Remarks;
            existing.StockItemId = model.StockItemId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToModel(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.StockPaymentInfo.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return false;

            _context.StockPaymentInfo.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.StockPaymentInfo.AnyAsync(x => x.Id == id);
        }

        private static StockPaymentInfo MapToModel(StockPaymentInfoEntity entity)
        {
            return new StockPaymentInfo
            {
                Id = entity.Id,
                No = entity.No,
                AccountName = entity.AccountName,
                AccountNo = entity.AccountNo,
                Ifsc = entity.Ifsc,
                Amount = entity.Amount,
                Date = entity.Date,
                PartyName = entity.PartyName,
                Bank = entity.Bank,
                InOut = entity.InOut,
                Remarks = entity.Remarks,
                StockItemId = entity.StockItemId,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static StockPaymentInfoEntity MapToEntity(StockPaymentInfo model)
        {
            return new StockPaymentInfoEntity
            {
                Id = model.Id,
                No = model.No,
                AccountName = model.AccountName,
                AccountNo = model.AccountNo,
                Ifsc = model.Ifsc,
                Amount = model.Amount,
                Date = model.Date,
                PartyName = model.PartyName,
                Bank = model.Bank,
                InOut = model.InOut,
                Remarks = model.Remarks,
                StockItemId = model.StockItemId,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }
}
