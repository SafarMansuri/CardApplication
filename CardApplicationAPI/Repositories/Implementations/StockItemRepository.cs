using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class StockItemRepository : IStockItemRepository
    {
        private readonly AppDbContext _context;

        public StockItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockItem>> GetAllAsync()
        {
            var entities = await _context.StockItems
                .Include(x => x.PaymentInfo)
                .ToListAsync();

            return entities.Select(MapToModel);
        }

        public async Task<StockItem?> GetByIdAsync(int id)
        {
            var entity = await _context.StockItems
                .Include(x => x.PaymentInfo)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity == null ? null : MapToModel(entity);
        }

        public async Task<StockItem> CreateAsync(StockItem model)
        {
            var entity = MapToEntity(model);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.StockItems.Add(entity);
            await _context.SaveChangesAsync();

            return MapToModel(entity);
        }

        public async Task<StockItem> UpdateAsync(StockItem model)
        {
            var existing = await _context.StockItems.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing == null)
                throw new InvalidOperationException($"Stock item with ID {model.Id} not found");

            existing.No = model.No;
            existing.InDate = model.InDate;
            existing.OutDate = model.OutDate;
            existing.Imei = model.Imei;
            existing.Colour = model.Colour;
            existing.Model = model.Model;
            existing.GstMrp = model.GstMrp;
            existing.Amount = model.Amount;
            existing.PartyName = model.PartyName;
            existing.Payment = model.Payment;
            existing.InOut = model.InOut;
            existing.Status = model.Status;
            existing.SoldTo = model.SoldTo;
            existing.Remarks = model.Remarks;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToModel(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.StockItems.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return false;

            _context.StockItems.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.StockItems.AnyAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<StockItem>> GetByPartyNameAsync(string partyName)
        {
            var entities = await _context.StockItems
                .Where(x => x.PartyName != null && x.PartyName.ToLower() == partyName.ToLower())
                .ToListAsync();

            return entities.Select(MapToModel);
        }

        public async Task<IEnumerable<StockItem>> GetByStatusAsync(string status)
        {
            var entities = await _context.StockItems
                .Where(x => x.Status != null && x.Status.ToLower() == status.ToLower())
                .ToListAsync();

            return entities.Select(MapToModel);
        }

        public async Task<IEnumerable<StockItem>> GetByInOutAsync(string inOut)
        {
            var entities = await _context.StockItems
                .Where(x => x.InOut != null && x.InOut.ToLower() == inOut.ToLower())
                .ToListAsync();

            return entities.Select(MapToModel);
        }

        private static StockItem MapToModel(StockItemEntity entity)
        {
            return new StockItem
            {
                Id = entity.Id,
                No = entity.No,
                InDate = entity.InDate,
                OutDate = entity.OutDate,
                Imei = entity.Imei,
                Colour = entity.Colour,
                Model = entity.Model,
                GstMrp = entity.GstMrp,
                Amount = entity.Amount,
                PartyName = entity.PartyName,
                Payment = entity.Payment,
                InOut = entity.InOut,
                Status = entity.Status,
                SoldTo = entity.SoldTo,
                Remarks = entity.Remarks,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static StockItemEntity MapToEntity(StockItem model)
        {
            return new StockItemEntity
            {
                Id = model.Id,
                No = model.No,
                InDate = model.InDate,
                OutDate = model.OutDate,
                Imei = model.Imei,
                Colour = model.Colour,
                Model = model.Model,
                GstMrp = model.GstMrp,
                Amount = model.Amount,
                PartyName = model.PartyName,
                Payment = model.Payment,
                InOut = model.InOut,
                Status = model.Status,
                SoldTo = model.SoldTo,
                Remarks = model.Remarks,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }
}
