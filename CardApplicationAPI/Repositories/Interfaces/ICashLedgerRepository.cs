using CardApplicationAPI.Models;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface ICashLedgerRepository
    {
        Task<IEnumerable<CashLedger>> GetAllAsync();
        Task<CashLedger?> GetByIdAsync(long id);
        Task<IEnumerable<CashLedger>> GetByTypeAsync(string transactionType);
        Task<CashLedger> CreateAsync(CashLedger model);
        Task<CashLedger> UpdateAsync(CashLedger model);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
        Task<decimal> GetOpeningBalanceAsync();
        Task<decimal> GetCashInAsync();
        Task<decimal> GetCashOutAsync();
        Task<decimal> GetCurrentBalanceAsync();
    }
}
