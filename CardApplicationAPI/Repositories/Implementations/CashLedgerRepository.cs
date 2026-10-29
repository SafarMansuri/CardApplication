using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class CashLedgerRepository : ICashLedgerRepository
    {
        private readonly AppDbContext _context;

        public CashLedgerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<CashLedger>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            string? transactionType = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var query = _context.CashLedger.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Name != null && x.Name.ToLower().Contains(term)) ||
                    (x.Remarks != null && x.Remarks.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(transactionType))
                query = query.Where(x => x.TransactionType == transactionType);

            var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sortBy?.ToLowerInvariant()) switch
            {
                "id" => asc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
                "transactiondate" => asc ? query.OrderBy(x => x.TransactionDate).ThenBy(x => x.Id) : query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id),
                "transactiontype" => asc ? query.OrderBy(x => x.TransactionType).ThenBy(x => x.Id) : query.OrderByDescending(x => x.TransactionType).ThenByDescending(x => x.Id),
                "name" => asc ? query.OrderBy(x => x.Name).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Name).ThenByDescending(x => x.Id),
                "amount" => asc ? query.OrderBy(x => x.Amount).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Amount).ThenByDescending(x => x.Id),
                "remarks" => asc ? query.OrderBy(x => x.Remarks).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Remarks).ThenByDescending(x => x.Id),
                "createdon" => asc ? query.OrderBy(x => x.CreatedOn).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Id),
                "createdby" => asc ? query.OrderBy(x => x.CreatedBy).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CreatedBy).ThenByDescending(x => x.Id),
                _ => sortDir == "desc"
                    ? query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id)
                    : sortDir == "asc"
                        ? query.OrderBy(x => x.TransactionDate).ThenBy(x => x.Id)
                        : query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id)
            };

            var page = await query.ToPaginatedResultAsync(pagination);

            return new PaginatedResult<CashLedger>
            {
                Items = page.Items.Select(MapToModel),
                Pagination = page.Pagination
            };
        }

        public async Task<CashLedger?> GetByIdAsync(long id)
        {
            var entity = await _context.CashLedger.FirstOrDefaultAsync(x => x.Id == id);
            return entity == null ? null : MapToModel(entity);
        }

        public async Task<PaginatedResult<CashLedger>> GetByTypeAsync(string transactionType, PaginationParams pagination)
        {
            var type = transactionType.Trim();
            var page = await _context.CashLedger
                .AsNoTracking()
                .Where(x => x.TransactionType == type)
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Id)
                .ToPaginatedResultAsync(pagination);

            return new PaginatedResult<CashLedger>
            {
                Items = page.Items.Select(MapToModel),
                Pagination = page.Pagination
            };
        }

        public async Task<CashLedger> CreateAsync(CashLedger model)
        {
            var entity = MapToEntity(model);
            entity.CreatedOn = DateTime.UtcNow;

            _context.CashLedger.Add(entity);
            await _context.SaveChangesAsync();

            return MapToModel(entity);
        }

        public async Task<CashLedger> UpdateAsync(CashLedger model)
        {
            var existing = await _context.CashLedger.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing == null)
                throw new KeyNotFoundException($"Cash ledger with ID {model.Id} not found");

            existing.TransactionDate = model.TransactionDate;
            existing.TransactionType = model.TransactionType;
            existing.Name = model.Name;
            existing.Amount = model.Amount;
            existing.Remarks = model.Remarks;
            existing.CreatedBy = model.CreatedBy;

            await _context.SaveChangesAsync();
            return MapToModel(existing);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.CashLedger.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return false;

            _context.CashLedger.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.CashLedger.AnyAsync(x => x.Id == id);
        }

        public async Task<decimal> GetOpeningBalanceAsync()
        {
            var opening = await _context.CashLedger
                .Where(x => x.TransactionType == "OPENING")
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            return opening;
        }

        public async Task<decimal> GetCashInAsync()
        {
            var totalIn = await _context.CashLedger
                .Where(x => x.TransactionType == "IN")
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            return totalIn;
        }

        public async Task<decimal> GetCashOutAsync()
        {
            var totalOut = await _context.CashLedger
                .Where(x => x.TransactionType == "OUT")
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            return totalOut;
        }

        public async Task<decimal> GetCurrentBalanceAsync()
        {
            var opening = await GetOpeningBalanceAsync();
            var cashIn = await GetCashInAsync();
            var cashOut = await GetCashOutAsync();

            return opening + cashIn - cashOut;
        }

        private static CashLedger MapToModel(CashLedgerEntity entity)
        {
            return new CashLedger
            {
                Id = entity.Id,
                TransactionDate = entity.TransactionDate,
                TransactionType = entity.TransactionType,
                Name = entity.Name,
                Amount = entity.Amount,
                Remarks = entity.Remarks,
                CreatedOn = entity.CreatedOn,
                CreatedBy = entity.CreatedBy
            };
        }

        private static CashLedgerEntity MapToEntity(CashLedger model)
        {
            return new CashLedgerEntity
            {
                Id = model.Id,
                TransactionDate = model.TransactionDate,
                TransactionType = model.TransactionType,
                Name = model.Name,
                Amount = model.Amount,
                Remarks = model.Remarks,
                CreatedOn = model.CreatedOn,
                CreatedBy = model.CreatedBy
            };
        }
    }
}
