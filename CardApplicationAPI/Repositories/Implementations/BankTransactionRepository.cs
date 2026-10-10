using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
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

        public async Task<IEnumerable<BankTransaction>> GetAllAsync()
        {
            var entities = await _context.BankTransactions
                .Include(x => x.Account)
                .Include(x => x.RelatedAccount)
                .OrderByDescending(x => x.TransactionDate)
                .ToListAsync();

            return entities.Select(MapToModel);
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
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var accountExists = await _context.BankAccounts.AnyAsync(x => x.Id == model.AccountId);
            if (!accountExists)
                throw new InvalidOperationException($"Bank account with ID {model.AccountId} not found");

            if (model.RelatedAccountId.HasValue)
            {
                var relatedExists = await _context.BankAccounts.AnyAsync(x => x.Id == model.RelatedAccountId.Value);
                if (!relatedExists)
                    throw new InvalidOperationException($"Related bank account with ID {model.RelatedAccountId.Value} not found");
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

        public async Task<BankTransaction> UpdateAsync(BankTransaction model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var existing = await _context.BankTransactions.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing == null)
                throw new InvalidOperationException($"Bank transaction with ID {model.Id} not found");

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
