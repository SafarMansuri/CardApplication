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

        public async Task<PaginatedResult<StockItem>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            string? status = null,
            string? inOut = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var query = _context.StockItems
                .AsNoTracking()
                .Include(x => x.PaymentInfo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    x.No.ToLower().Contains(term) ||
                    (x.Model != null && x.Model.ToLower().Contains(term)) ||
                    (x.Imei != null && x.Imei.ToLower().Contains(term)) ||
                    (x.PartyName != null && x.PartyName.ToLower().Contains(term)) ||
                    (x.SoldTo != null && x.SoldTo.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(x => x.Status != null && x.Status.ToLower() == normalizedStatus);
            }

            if (!string.IsNullOrWhiteSpace(inOut))
            {
                var normalizedInOut = inOut.Trim().ToUpper();
                query = query.Where(x => x.InOut != null && x.InOut.ToUpper() == normalizedInOut);
            }

            var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            query = ApplySort(query, sortBy, asc);

            var result = await query.ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockItem> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        private static IQueryable<StockItemEntity> ApplySort(IQueryable<StockItemEntity> query, string? sortBy, bool asc)
        {
            return (sortBy?.ToLowerInvariant()) switch
            {
                "id" => asc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
                "no" => asc ? query.OrderBy(x => x.No).ThenBy(x => x.Id) : query.OrderByDescending(x => x.No).ThenByDescending(x => x.Id),
                "indate" => asc ? query.OrderBy(x => x.InDate).ThenBy(x => x.Id) : query.OrderByDescending(x => x.InDate).ThenByDescending(x => x.Id),
                "outdate" => asc ? query.OrderBy(x => x.OutDate).ThenBy(x => x.Id) : query.OrderByDescending(x => x.OutDate).ThenByDescending(x => x.Id),
                "imei" => asc ? query.OrderBy(x => x.Imei).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Imei).ThenByDescending(x => x.Id),
                "colour" => asc ? query.OrderBy(x => x.Colour).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Colour).ThenByDescending(x => x.Id),
                "model" => asc ? query.OrderBy(x => x.Model).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Model).ThenByDescending(x => x.Id),
                "gstmrp" => asc ? query.OrderBy(x => x.GstMrp).ThenBy(x => x.Id) : query.OrderByDescending(x => x.GstMrp).ThenByDescending(x => x.Id),
                "amount" => asc ? query.OrderBy(x => x.Amount).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Amount).ThenByDescending(x => x.Id),
                "partyname" => asc ? query.OrderBy(x => x.PartyName).ThenBy(x => x.Id) : query.OrderByDescending(x => x.PartyName).ThenByDescending(x => x.Id),
                "payment" => asc ? query.OrderBy(x => x.Payment).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Payment).ThenByDescending(x => x.Id),
                "inout" => asc ? query.OrderBy(x => x.InOut).ThenBy(x => x.Id) : query.OrderByDescending(x => x.InOut).ThenByDescending(x => x.Id),
                "status" => asc ? query.OrderBy(x => x.Status).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Status).ThenByDescending(x => x.Id),
                "soldto" => asc ? query.OrderBy(x => x.SoldTo).ThenBy(x => x.Id) : query.OrderByDescending(x => x.SoldTo).ThenByDescending(x => x.Id),
                "remarks" => asc ? query.OrderBy(x => x.Remarks).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Remarks).ThenByDescending(x => x.Id),
                "paymentstatus" => asc
                    ? query.OrderBy(x => x.PaymentInfo.Select(p => p.PaymentStatus).FirstOrDefault() ?? "Pending").ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.PaymentInfo.Select(p => p.PaymentStatus).FirstOrDefault() ?? "Pending").ThenByDescending(x => x.Id),
                "createdat" => asc ? query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
                "updatedat" => asc ? query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Id) : query.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id),
                _ => asc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id)
            };
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
                await _context.Entry(entity).Collection(x => x.PaymentInfo).LoadAsync();
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
                await _context.StockPaymentInfo
                    .FromSqlInterpolated($"SELECT * FROM [StockPaymentInfo] WITH (UPDLOCK, HOLDLOCK) WHERE [StockItemId] = {model.Id}")
                    .LoadAsync();
                var existing = await _context.StockItems
                    .Include(x => x.PaymentInfo)
                    .FirstOrDefaultAsync(x => x.Id == model.Id);
                if (existing == null)
                    throw new InvalidOperationException($"Stock item with ID {model.Id} not found");

                var completedPayment = existing.PaymentInfo.FirstOrDefault(x => x.PaymentStatus == "Completed");
                var financialFieldsChanged = existing.Amount != model.Amount
                    || !string.Equals(existing.InOut, model.InOut, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(existing.PartyName?.Trim(), model.PartyName?.Trim(), StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(existing.SoldTo?.Trim(), model.SoldTo?.Trim(), StringComparison.OrdinalIgnoreCase);
                if (completedPayment != null && financialFieldsChanged)
                    throw new InvalidOperationException("Reset the completed stock payment before changing financial fields.");

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
                await _context.StockPaymentInfo
                    .FromSqlInterpolated($"SELECT * FROM [StockPaymentInfo] WITH (UPDLOCK, HOLDLOCK) WHERE [StockItemId] = {id}")
                    .LoadAsync();
                var entity = await _context.StockItems
                    .Include(x => x.PaymentInfo)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null) return false;
                if (entity.PaymentInfo.Any(x => x.PaymentStatus == "Completed"))
                    throw new InvalidOperationException("Reset the completed stock payment before deleting this stock item.");

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
            var result = await _context.StockItems
                .Include(x => x.PaymentInfo)
                .Where(x => x.PartyName != null && x.PartyName.ToLower() == partyName.ToLower())
                .OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockItem> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<PaginatedResult<StockItem>> GetByStatusAsync(string status, PaginationParams pagination)
        {
            var result = await _context.StockItems
                .Include(x => x.PaymentInfo)
                .Where(x => x.Status != null && x.Status.ToLower() == status.ToLower())
                .OrderByDescending(x => x.Id).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<StockItem> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<PaginatedResult<StockItem>> GetByInOutAsync(string inOut, PaginationParams pagination)
        {
            var result = await _context.StockItems
                .Include(x => x.PaymentInfo)
                .Where(x => x.InOut != null && x.InOut.ToLower() == inOut.ToLower())
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
                PaymentStatus = entity.PaymentInfo?.OrderByDescending(x => x.Id).FirstOrDefault()?.PaymentStatus ?? "Pending",
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

            var isReceivable = string.Equals(stockItem.InOut, "OUT", StringComparison.OrdinalIgnoreCase);
            var responsibleParty = isReceivable ? stockItem.SoldTo : stockItem.PartyName;
            summary.No = stockItem.No;
            summary.AccountName = responsibleParty;
            summary.AccountNo = null;
            summary.Ifsc = null;
            summary.Amount = stockItem.Amount;
            summary.Date = stockItem.InDate ?? stockItem.OutDate ?? stockItem.CreatedAt;
            summary.PartyName = responsibleParty;
            summary.Bank = stockItem.Payment;
            summary.InOut = stockItem.InOut;
            summary.PaymentType = isReceivable ? "Receivable" : "Payable";
            summary.Remarks = stockItem.Remarks;
            summary.StockItemId = stockItem.Id;

            if (existingSummary == null)
            {
                summary.PaymentStatus = "Pending";
                summary.OutstandingAmount = stockItem.Amount ?? 0m;
                summary.CreatedAt = DateTime.UtcNow;
                _context.StockPaymentInfo.Add(summary);
            }
            else if (summary.PaymentStatus == "Pending")
            {
                summary.OutstandingAmount = stockItem.Amount ?? 0m;
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
