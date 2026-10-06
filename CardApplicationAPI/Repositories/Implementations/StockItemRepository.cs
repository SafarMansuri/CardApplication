using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
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

        public async Task<PaginatedResult<StockItem>> GetAllAsync(PaginationParams pagination)
        {
            var result = await _context.StockItems
                .Include(x => x.PaymentInfo)
                .OrderByDescending(x => x.Id)
                .ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockItem> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
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
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = MapToEntity(model);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.PartyName = model.PartyName?.Trim();

                _context.StockItems.Add(entity);
                await _context.SaveChangesAsync();
                await SyncStockPaymentInfoForStockItemAsync(entity.Id);
                await transaction.CommitAsync();

                return MapToModel(entity);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StockItem> UpdateAsync(StockItem model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
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
                existing.PartyName = model.PartyName?.Trim();
                existing.Payment = model.Payment;
                existing.InOut = model.InOut;
                existing.Status = model.Status;
                existing.SoldTo = model.SoldTo;
                existing.Remarks = model.Remarks;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await SyncStockPaymentInfoForStockItemAsync(existing.Id);
                await transaction.CommitAsync();
                return MapToModel(existing);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.StockItems.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null) return false;

                var stockItemId = entity.Id;
                _context.StockItems.Remove(entity);
                await _context.SaveChangesAsync();
                await RemoveStockPaymentInfoForStockItemAsync(stockItemId);
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.StockItems.AnyAsync(x => x.Id == id);
        }

        public async Task<PaginatedResult<StockItem>> GetByPartyNameAsync(string partyName, PaginationParams pagination)
        {
            var result = await _context.StockItems.Where(x => x.PartyName != null && x.PartyName.ToLower() == partyName.ToLower())
                .OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockItem> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<PaginatedResult<StockItem>> GetByStatusAsync(string status, PaginationParams pagination)
        {
            var result = await _context.StockItems.Where(x => x.Status != null && x.Status.ToLower() == status.ToLower())
                .OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockItem> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<PaginatedResult<StockItem>> GetByInOutAsync(string inOut, PaginationParams pagination)
        {
            var result = await _context.StockItems.Where(x => x.InOut != null && x.InOut.ToLower() == inOut.ToLower())
                .OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockItem> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
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

        private async Task SyncStockPaymentInfoForStockItemAsync(int stockItemId)
        {
            var stockItem = await _context.StockItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == stockItemId);
            if (stockItem == null)
            {
                await RemoveStockPaymentInfoForStockItemAsync(stockItemId);
                return;
            }

            var summaries = await _context.StockPaymentInfo
                .Where(x => x.StockItemId == stockItemId)
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (summaries.Count > 1)
            {
                _context.StockPaymentInfo.RemoveRange(summaries.Skip(1));
                await _context.SaveChangesAsync();
                summaries = summaries.Take(1).ToList();
            }

            var existingSummary = summaries.FirstOrDefault();
            var summary = existingSummary ?? new StockPaymentInfoEntity();

            summary.No = stockItem.No;
            summary.AccountName = stockItem.PartyName;
            summary.AccountNo = null;
            summary.Ifsc = null;
            summary.Amount = stockItem.Amount;
            summary.Date = stockItem.InDate ?? stockItem.OutDate ?? stockItem.CreatedAt;
            summary.PartyName = stockItem.PartyName;
            summary.Bank = stockItem.Payment;
            summary.InOut = stockItem.InOut;
            summary.Remarks = stockItem.Remarks;
            summary.StockItemId = stockItem.Id;

            if (existingSummary == null)
            {
                summary.CreatedAt = DateTime.UtcNow;
                _context.StockPaymentInfo.Add(summary);
            }

            summary.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task RemoveStockPaymentInfoForStockItemAsync(int stockItemId)
        {
            var summaries = await _context.StockPaymentInfo
                .Where(x => x.StockItemId == stockItemId)
                .ToListAsync();

            if (!summaries.Any())
                return;

            _context.StockPaymentInfo.RemoveRange(summaries);
            await _context.SaveChangesAsync();
        }
    }
}
