using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
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

        public async Task<IEnumerable<BankAccount>> GetAllAsync()
        {
            var entities = await _context.BankAccounts
                .OrderBy(x => x.AccountName)
                .ToListAsync();

            return entities.Select(MapToModel);
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
                throw new InvalidOperationException($"Bank account with ID {model.Id} not found");

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

            _context.BankAccounts.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.BankAccounts.AnyAsync(x => x.Id == id);
        }

        public async Task<decimal> GetTotalBalanceAsync()
        {
            var summary = await GetAccountBalanceSummaryAsync();
            return summary.Sum(x => x.CurrentBalance);
        }

        public async Task<IEnumerable<BankAccountBalanceSummary>> GetAccountBalanceSummaryAsync()
        {
            var accounts = await _context.BankAccounts
                .AsNoTracking()
                .OrderBy(x => x.AccountName)
                .ToListAsync();

            var summaries = new List<BankAccountBalanceSummary>();

            foreach (var account in accounts)
            {
                var transactions = await _context.BankTransactions
                    .AsNoTracking()
                    .Where(x => x.AccountId == account.Id)
                    .ToListAsync();

                var openingBalance = transactions.Where(x => x.TransactionType == "OPENING").Sum(x => x.Amount);
                var totalIn = transactions.Where(x => x.TransactionType == "IN").Sum(x => x.Amount);
                var totalOut = transactions.Where(x => x.TransactionType == "OUT").Sum(x => x.Amount);
                var transferIn = transactions.Where(x => x.TransactionType == "TRANSFER_IN").Sum(x => x.Amount);
                var transferOut = transactions.Where(x => x.TransactionType == "TRANSFER_OUT").Sum(x => x.Amount);

                var currentBalance = openingBalance + totalIn + transferIn - totalOut - transferOut;

                summaries.Add(new BankAccountBalanceSummary
                {
                    AccountId = account.Id,
                    AccountName = account.AccountName,
                    BankName = account.BankName,
                    OpeningBalance = openingBalance,
                    TotalIn = totalIn,
                    TotalOut = totalOut,
                    TransferIn = transferIn,
                    TransferOut = transferOut,
                    CurrentBalance = currentBalance
                });
            }

            return summaries;
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
