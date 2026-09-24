using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
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

        public async Task<IEnumerable<StockPaymentInfo>> GetAllAsync()
        {
            var entities = await _context.StockPaymentInfo
                .Include(x => x.StockItem)
                .ToListAsync();

            return entities.Select(MapToModel);
        }

        public async Task<StockPaymentInfo?> GetByIdAsync(int id)
        {
            var entity = await _context.StockPaymentInfo
                .Include(x => x.StockItem)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity == null ? null : MapToModel(entity);
        }

        public async Task<IEnumerable<StockPaymentInfo>> GetByStockItemIdAsync(int stockItemId)
        {
            var entities = await _context.StockPaymentInfo
                .Where(x => x.StockItemId == stockItemId)
                .ToListAsync();

            return entities.Select(MapToModel);
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
                Remarks = model.Remarks,
                StockItemId = model.StockItemId,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }
}
