using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class BankTransactionRepository : IBankTransactionRepository
    {
        private readonly AppDbContext _context;

        public BankTransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<BankTransaction>> GetAllAsync(PaginationParams pagination)
        {
            var result = await _context.BankTransactions
                .AsNoTracking()
                .Include(x => x.Account)
                .Include(x => x.RelatedAccount)
                .OrderByDescending(x => x.TransactionDate)
                .ThenByDescending(x => x.Id)
                .ToPaginatedResultAsync(pagination);

            return new PaginatedResult<BankTransaction>
            {
                Items = result.Items.Select(MapToModel),
                Pagination = result.Pagination
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

        public async Task<IEnumerable<BankTransaction>> GetByAccountIdAsync(int accountId)
        {
            var entities = await _context.BankTransactions
                .Include(x => x.Account)
                .Include(x => x.RelatedAccount)
                .Where(x => x.AccountId == accountId)
                .OrderByDescending(x => x.TransactionDate)
                .ToListAsync();

            return entities.Select(MapToModel);
        }

        public async Task<BankTransaction> CreateAsync(BankTransaction model)
        {
            var accountExists = await _context.BankAccounts.AnyAsync(x => x.Id == model.AccountId);
            if (!accountExists)
                throw new InvalidOperationException($"Bank account with ID {model.AccountId} not found");

            if (model.RelatedAccountId.HasValue)
            {
                var relatedExists = await _context.BankAccounts.AnyAsync(x => x.Id == model.RelatedAccountId.Value);
                if (!relatedExists)
                    throw new InvalidOperationException($"Related bank account with ID {model.RelatedAccountId.Value} not found");
            }

            var entity = MapToEntity(model);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.BankTransactions.Add(entity);
            await _context.SaveChangesAsync();

            return MapToModel(entity);
        }

        public async Task<BankTransferResult?> TransferAsync(BankTransferRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var accountIds = await _context.BankAccounts
                .Where(x => x.Id == request.FromAccountId || x.Id == request.ToAccountId)
                .Select(x => x.Id)
                .ToListAsync();

            if (!accountIds.Contains(request.FromAccountId) || !accountIds.Contains(request.ToAccountId))
                return null;

            var createdAt = DateTime.UtcNow;
            var sourceEntity = new BankTransactionEntity
            {
                AccountId = request.FromAccountId,
                RelatedAccountId = request.ToAccountId,
                TransactionDate = request.TransactionDate,
                TransactionType = "TRANSFER_OUT",
                Amount = request.Amount,
                ReferenceNo = request.ReferenceNo,
                Remarks = request.Remarks,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };
            var destinationEntity = new BankTransactionEntity
            {
                AccountId = request.ToAccountId,
                RelatedAccountId = request.FromAccountId,
                TransactionDate = request.TransactionDate,
                TransactionType = "TRANSFER_IN",
                Amount = request.Amount,
                ReferenceNo = request.ReferenceNo,
                Remarks = request.Remarks,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            _context.BankTransactions.AddRange(sourceEntity, destinationEntity);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new BankTransferResult
            {
                SourceTransaction = MapToModel(sourceEntity),
                DestinationTransaction = MapToModel(destinationEntity)
            };
        }

        public async Task<BankTransaction> UpdateAsync(BankTransaction model)
        {
            var existing = await _context.BankTransactions.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing == null)
                throw new InvalidOperationException($"Bank transaction with ID {model.Id} not found");

            existing.AccountId = model.AccountId;
            existing.RelatedAccountId = model.RelatedAccountId;
            existing.TransactionDate = model.TransactionDate;
            existing.TransactionType = model.TransactionType;
            existing.Amount = model.Amount;
            existing.ReferenceNo = model.ReferenceNo;
            existing.Remarks = model.Remarks;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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
