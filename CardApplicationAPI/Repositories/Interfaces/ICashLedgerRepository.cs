using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface ICashLedgerRepository
    {
        Task<PaginatedResult<CashLedger>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            string? transactionType = null,
            string? sortBy = null,
            string? sortDir = null);
        Task<CashLedger?> GetByIdAsync(long id);
        Task<PaginatedResult<CashLedger>> GetByTypeAsync(string transactionType, PaginationParams pagination);
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
