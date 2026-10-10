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

        public async Task<PaginatedResult<CashLedger>> GetAllAsync(PaginationParams pagination)
        {
            var page = await _context.CashLedger
                .AsNoTracking()
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Id)
                .ToPaginatedResultAsync(pagination);

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
                throw new InvalidOperationException($"Cash ledger with ID {model.Id} not found");

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
