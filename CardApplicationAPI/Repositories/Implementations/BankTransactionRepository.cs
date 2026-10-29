using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class BankTransactionRepository : IBankTransactionRepository
    {
        private readonly AppDbContext _context;

        public BankTransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BankTransaction>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            IReadOnlyCollection<string>? transactionTypes = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var query = _context.BankTransactions
                .AsNoTracking()
                .Include(x => x.Account)
                .Include(x => x.RelatedAccount)
                .AsQueryable();

            query = ApplyFiltersAndSort(query, search, transactionTypes, sortBy, sortDir);
            var page = await query.ToPaginatedResultAsync(pagination);

            return new PaginatedResult<BankTransaction>
            {
                Items = page.Items.Select(MapToModel),
                Pagination = page.Pagination
            };
        }

        public async Task<BankTransaction?> GetByIdAsync(int id)
        {
            var entity = await _context.BankTransactions
                .Include(x => x.Account)
                .Include(x => x.RelatedAccount)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity == null ? null : MapToModel(entity);
        }

        public async Task<PaginatedResult<BankTransaction>> GetByAccountIdAsync(
            int accountId,
            PaginationParams pagination,
            string? search = null,
            IReadOnlyCollection<string>? transactionTypes = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var query = _context.BankTransactions
                .AsNoTracking()
                .Include(x => x.Account)
                .Include(x => x.RelatedAccount)
                .Where(x => x.AccountId == accountId);

            query = ApplyFiltersAndSort(query, search, transactionTypes, sortBy, sortDir);
            var page = await query.ToPaginatedResultAsync(pagination);

            return new PaginatedResult<BankTransaction>
            {
                Items = page.Items.Select(MapToModel),
                Pagination = page.Pagination
            };
        }

        private static IQueryable<BankTransactionEntity> ApplyFiltersAndSort(
            IQueryable<BankTransactionEntity> query,
            string? search,
            IReadOnlyCollection<string>? transactionTypes,
            string? sortBy,
            string? sortDir)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Remarks != null && x.Remarks.ToLower().Contains(term)) ||
                    (x.ReferenceNo != null && x.ReferenceNo.ToLower().Contains(term)) ||
                    (x.Account != null && (x.Account.BankName.ToLower().Contains(term) || x.Account.AccountName.ToLower().Contains(term))) ||
                    (x.RelatedAccount != null && (x.RelatedAccount.BankName.ToLower().Contains(term) || x.RelatedAccount.AccountName.ToLower().Contains(term))));
            }

            if (transactionTypes != null && transactionTypes.Count > 0)
            {
                var typeArray = transactionTypes.ToArray();
                query = query.Where(x => typeArray.Contains(x.TransactionType));
            }

            var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            return (sortBy?.ToLowerInvariant()) switch
            {
                "id" => asc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
                "accountid" => asc ? query.OrderBy(x => x.AccountId).ThenBy(x => x.Id) : query.OrderByDescending(x => x.AccountId).ThenByDescending(x => x.Id),
                "relatedaccountid" => asc ? query.OrderBy(x => x.RelatedAccountId).ThenBy(x => x.Id) : query.OrderByDescending(x => x.RelatedAccountId).ThenByDescending(x => x.Id),
                "transactiondate" => asc ? query.OrderBy(x => x.TransactionDate).ThenBy(x => x.Id) : query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id),
                "transactiontype" => asc ? query.OrderBy(x => x.TransactionType).ThenBy(x => x.Id) : query.OrderByDescending(x => x.TransactionType).ThenByDescending(x => x.Id),
                "amount" => asc ? query.OrderBy(x => x.Amount).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Amount).ThenByDescending(x => x.Id),
                "referenceno" => asc ? query.OrderBy(x => x.ReferenceNo).ThenBy(x => x.Id) : query.OrderByDescending(x => x.ReferenceNo).ThenByDescending(x => x.Id),
                "remarks" => asc ? query.OrderBy(x => x.Remarks).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Remarks).ThenByDescending(x => x.Id),
                "createdat" => asc ? query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
                "updatedat" => asc ? query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Id) : query.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id),
                "accountname" => asc
                    ? query.OrderBy(x => x.Account != null ? x.Account.AccountName : null).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.Account != null ? x.Account.AccountName : null).ThenByDescending(x => x.Id),
                "bankname" => asc
                    ? query.OrderBy(x => x.Account != null ? x.Account.BankName : null).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.Account != null ? x.Account.BankName : null).ThenByDescending(x => x.Id),
                _ => asc
                    ? query.OrderBy(x => x.TransactionDate).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id)
            };
        }

        public async Task<BankTransaction> CreateAsync(BankTransaction model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var accountExists = await _context.BankAccounts.AnyAsync(x => x.Id == model.AccountId);
            if (!accountExists)
                throw new KeyNotFoundException($"Bank account with ID {model.AccountId} not found");

            if (model.RelatedAccountId.HasValue)
            {
                var relatedExists = await _context.BankAccounts.AnyAsync(x => x.Id == model.RelatedAccountId.Value);
                if (!relatedExists)
                    throw new KeyNotFoundException($"Related bank account with ID {model.RelatedAccountId.Value} not found");
            }

            if (model.TransactionType == "TRANSFER_OUT")
                await EnsureSufficientBalanceForTransferOutAsync(model.AccountId, model.Amount);

            var entity = MapToEntity(model);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.BankTransactions.Add(entity);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToModel(entity);
        }

        public async Task<BankTransferResult> TransferAsync(BankTransferRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            if (request.FromAccountId == request.ToAccountId)
                throw new ArgumentException("Source and destination accounts must be different.");

            foreach (var accountId in new[] { request.FromAccountId, request.ToAccountId }.Order())
                await _context.BankAccounts
                    .FromSqlInterpolated($"SELECT * FROM [BankAccounts] WITH (UPDLOCK) WHERE [Id] = {accountId}")
                    .LoadAsync();

            var sourceAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(x => x.Id == request.FromAccountId);
            if (sourceAccount == null)
                throw new KeyNotFoundException($"Source bank account with ID {request.FromAccountId} was not found.");

            var destinationAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(x => x.Id == request.ToAccountId);
            if (destinationAccount == null)
                throw new KeyNotFoundException($"Destination bank account with ID {request.ToAccountId} was not found.");

            await EnsureSufficientBalanceForTransferOutAsync(request.FromAccountId, request.Amount);

            var createdAt = DateTime.UtcNow;
            var sourceTransaction = new BankTransactionEntity
            {
                AccountId = sourceAccount.Id,
                RelatedAccountId = destinationAccount.Id,
                TransactionDate = request.TransactionDate,
                TransactionType = "TRANSFER_OUT",
                Amount = request.Amount,
                ReferenceNo = request.ReferenceNo,
                Remarks = request.Remarks,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                Account = sourceAccount,
                RelatedAccount = destinationAccount
            };

            var destinationTransaction = new BankTransactionEntity
            {
                AccountId = destinationAccount.Id,
                RelatedAccountId = sourceAccount.Id,
                TransactionDate = request.TransactionDate,
                TransactionType = "TRANSFER_IN",
                Amount = request.Amount,
                ReferenceNo = request.ReferenceNo,
                Remarks = request.Remarks,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                Account = destinationAccount,
                RelatedAccount = sourceAccount
            };

            _context.BankTransactions.AddRange(sourceTransaction, destinationTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new BankTransferResult
            {
                SourceTransaction = MapToModel(sourceTransaction),
                DestinationTransaction = MapToModel(destinationTransaction)
            };
        }

        public async Task<BankTransaction> UpdateAsync(BankTransaction model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var existing = await _context.BankTransactions.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing == null)
                throw new KeyNotFoundException($"Bank transaction with ID {model.Id} not found");

            if (model.TransactionType == "TRANSFER_OUT")
                await EnsureSufficientBalanceForTransferOutAsync(model.AccountId, model.Amount, model.Id);

            existing.AccountId = model.AccountId;
            existing.RelatedAccountId = model.RelatedAccountId;
            existing.TransactionDate = model.TransactionDate;
            existing.TransactionType = model.TransactionType;
            existing.Amount = model.Amount;
            existing.ReferenceNo = model.ReferenceNo;
            existing.Remarks = model.Remarks;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return MapToModel(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.BankTransactions.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return false;

            _context.BankTransactions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.BankTransactions.AnyAsync(x => x.Id == id);
        }

        private async Task EnsureSufficientBalanceForTransferOutAsync(int accountId, decimal transferAmount, int? excludedTransactionId = null)
        {
            var transactions = _context.BankTransactions
                .Where(x => x.AccountId == accountId);

            if (excludedTransactionId.HasValue)
                transactions = transactions.Where(x => x.Id != excludedTransactionId.Value);

            var availableBalance = await transactions.SumAsync(x =>
                x.TransactionType == "OPENING" || x.TransactionType == "IN" || x.TransactionType == "TRANSFER_IN"
                    ? x.Amount
                    : -x.Amount);

            if (transferAmount > availableBalance)
                throw new ArgumentException($"Insufficient balance for transfer. Available balance: {availableBalance:0.00}.");
        }

        private static BankTransaction MapToModel(BankTransactionEntity entity)
        {
            return new BankTransaction
            {
                Id = entity.Id,
                AccountId = entity.AccountId,
                RelatedAccountId = entity.RelatedAccountId,
                TransactionDate = entity.TransactionDate,
                TransactionType = entity.TransactionType,
                Amount = entity.Amount,
                ReferenceNo = entity.ReferenceNo,
                Remarks = entity.Remarks,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Account = entity.Account == null ? null : new BankAccount
                {
                    Id = entity.Account.Id,
                    AccountName = entity.Account.AccountName,
                    BankName = entity.Account.BankName,
                    AccountNo = entity.Account.AccountNo,
                    IsActive = entity.Account.IsActive,
                    CreatedAt = entity.Account.CreatedAt,
                    UpdatedAt = entity.Account.UpdatedAt
                },
                RelatedAccount = entity.RelatedAccount == null ? null : new BankAccount
                {
                    Id = entity.RelatedAccount.Id,
                    AccountName = entity.RelatedAccount.AccountName,
                    BankName = entity.RelatedAccount.BankName,
                    AccountNo = entity.RelatedAccount.AccountNo,
                    IsActive = entity.RelatedAccount.IsActive,
                    CreatedAt = entity.RelatedAccount.CreatedAt,
                    UpdatedAt = entity.RelatedAccount.UpdatedAt
                }
            };
        }

        private static BankTransactionEntity MapToEntity(BankTransaction model)
        {
            return new BankTransactionEntity
            {
                Id = model.Id,
                AccountId = model.AccountId,
                RelatedAccountId = model.RelatedAccountId,
                TransactionDate = model.TransactionDate,
                TransactionType = model.TransactionType,
                Amount = model.Amount,
                ReferenceNo = model.ReferenceNo,
                Remarks = model.Remarks,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }
}
