using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class BankAccountRepository : IBankAccountRepository
    {
        private readonly AppDbContext _context;

        public BankAccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BankAccount>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            bool? isActive = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var query = _context.BankAccounts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    x.BankName.ToLower().Contains(term) ||
                    x.AccountName.ToLower().Contains(term) ||
                    (x.AccountNo != null && x.AccountNo.ToLower().Contains(term)));
            }

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sortBy?.ToLowerInvariant()) switch
            {
                "id" => asc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
                "accountname" => asc ? query.OrderBy(x => x.AccountName).ThenBy(x => x.Id) : query.OrderByDescending(x => x.AccountName).ThenByDescending(x => x.Id),
                "bankname" => asc ? query.OrderBy(x => x.BankName).ThenBy(x => x.Id) : query.OrderByDescending(x => x.BankName).ThenByDescending(x => x.Id),
                "accountno" => asc ? query.OrderBy(x => x.AccountNo).ThenBy(x => x.Id) : query.OrderByDescending(x => x.AccountNo).ThenByDescending(x => x.Id),
                "isactive" => asc ? query.OrderBy(x => x.IsActive).ThenBy(x => x.Id) : query.OrderByDescending(x => x.IsActive).ThenByDescending(x => x.Id),
                "createdat" => asc ? query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
                "updatedat" => asc ? query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Id) : query.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id),
                _ => sortDir == "asc"
                    ? query.OrderBy(x => x.Id)
                    : sortDir == "desc"
                        ? query.OrderByDescending(x => x.Id)
                        : query.OrderBy(x => x.AccountName).ThenBy(x => x.Id)
            };

            var page = await query.ToPaginatedResultAsync(pagination);

            return new PaginatedResult<BankAccount>
            {
                Items = page.Items.Select(MapToModel),
                Pagination = page.Pagination
            };
        }

        public async Task<BankAccount?> GetByIdAsync(int id)
        {
            var entity = await _context.BankAccounts.FirstOrDefaultAsync(x => x.Id == id);
            return entity == null ? null : MapToModel(entity);
        }

        public async Task<BankAccount> CreateAsync(BankAccount model)
        {
            var entity = MapToEntity(model);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.BankAccounts.Add(entity);
            await _context.SaveChangesAsync();
            return MapToModel(entity);
        }

        public async Task<BankAccount> UpdateAsync(BankAccount model)
        {
            var existing = await _context.BankAccounts.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing == null)
                throw new KeyNotFoundException($"Bank account with ID {model.Id} not found");

            existing.AccountName = model.AccountName.Trim();
            existing.BankName = model.BankName.Trim();
            existing.AccountNo = model.AccountNo;
            existing.IsActive = model.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToModel(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.BankAccounts.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return false;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _context.BankTransactions
                .Where(x => x.RelatedAccountId == id && x.AccountId != id)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.RelatedAccountId, (int?)null));

            await _context.BankTransactions
                .Where(x => x.AccountId == id)
                .ExecuteDeleteAsync();

            _context.BankAccounts.Remove(entity);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.BankAccounts.AnyAsync(x => x.Id == id);
        }

        public async Task<decimal> GetTotalBalanceAsync()
        {
            return await _context.BankTransactions
                .AsNoTracking()
                .SumAsync(x => (decimal?)(x.TransactionType == "OPENING" || x.TransactionType == "IN" || x.TransactionType == "TRANSFER_IN"
                    ? x.Amount
                    : -x.Amount)) ?? 0m;
        }

        public async Task<PaginatedResult<BankAccountBalanceSummary>> GetAccountBalanceSummaryAsync(PaginationParams pagination)
        {
            var accountPage = await _context.BankAccounts
                .AsNoTracking()
                .OrderBy(x => x.AccountName)
                .ThenBy(x => x.Id)
                .ToPaginatedResultAsync(pagination);
            var accountIds = accountPage.Items.Select(x => x.Id).ToArray();

            var transactionSummaries = await _context.BankTransactions
                .AsNoTracking()
                .Where(x => accountIds.Contains(x.AccountId))
                .GroupBy(x => x.AccountId)
                .Select(group => new
                {
                    AccountId = group.Key,
                    OpeningBalance = group.Sum(x => x.TransactionType == "OPENING" ? x.Amount : 0m),
                    TotalIn = group.Sum(x => x.TransactionType == "IN" ? x.Amount : 0m),
                    TotalOut = group.Sum(x => x.TransactionType == "OUT" ? x.Amount : 0m),
                    TransferIn = group.Sum(x => x.TransactionType == "TRANSFER_IN" ? x.Amount : 0m),
                    TransferOut = group.Sum(x => x.TransactionType == "TRANSFER_OUT" ? x.Amount : 0m)
                })
                .ToDictionaryAsync(x => x.AccountId);

            var summaries = accountPage.Items.Select(account =>
            {
                transactionSummaries.TryGetValue(account.Id, out var totals);
                var openingBalance = totals?.OpeningBalance ?? 0m;
                var totalIn = totals?.TotalIn ?? 0m;
                var totalOut = totals?.TotalOut ?? 0m;
                var transferIn = totals?.TransferIn ?? 0m;
                var transferOut = totals?.TransferOut ?? 0m;

                return new BankAccountBalanceSummary
                {
                    AccountId = account.Id,
                    AccountName = account.AccountName,
                    BankName = account.BankName,
                    OpeningBalance = openingBalance,
                    TotalIn = totalIn,
                    TotalOut = totalOut,
                    TransferIn = transferIn,
                    TransferOut = transferOut,
                    CurrentBalance = openingBalance + totalIn + transferIn - totalOut - transferOut
                };
            }).ToList();

            return new PaginatedResult<BankAccountBalanceSummary>
            {
                Items = summaries,
                Pagination = accountPage.Pagination
            };
        }

        public async Task<BankAccountStatusSummary> GetStatusSummaryAsync()
        {
            var counts = await _context.BankAccounts
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Active = g.Count(x => x.IsActive),
                    Inactive = g.Count(x => !x.IsActive)
                })
                .FirstOrDefaultAsync();

            return counts == null
                ? new BankAccountStatusSummary(0, 0, 0)
                : new BankAccountStatusSummary(counts.Total, counts.Active, counts.Inactive);
        }

        private static BankAccount MapToModel(BankAccountEntity entity)
        {
            return new BankAccount
            {
                Id = entity.Id,
                AccountName = entity.AccountName,
                BankName = entity.BankName,
                AccountNo = entity.AccountNo,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static BankAccountEntity MapToEntity(BankAccount model)
        {
            return new BankAccountEntity
            {
                Id = model.Id,
                AccountName = model.AccountName,
                BankName = model.BankName,
                AccountNo = model.AccountNo,
                IsActive = model.IsActive,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }
}
