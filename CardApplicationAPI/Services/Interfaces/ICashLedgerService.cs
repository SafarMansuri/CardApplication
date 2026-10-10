using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface ICashLedgerService
    {
        Task<InternalActionResult<PaginatedResult<CashLedger>>> GetAllAsync(PaginationParams pagination);
        Task<InternalActionResult<CashLedger?>> GetByIdAsync(long id);
        Task<InternalActionResult<PaginatedResult<CashLedger>>> GetByTypeAsync(string transactionType, PaginationParams pagination);
        Task<InternalActionResult<CashLedger>> AddAsync(CashLedger model);
        Task<InternalActionResult<bool>> UpdateAsync(CashLedger model);
        Task<InternalActionResult<bool>> DeleteAsync(long id);
        Task<InternalActionResult<decimal>> GetOpeningBalanceAsync();
        Task<InternalActionResult<decimal>> GetCashInAsync();
        Task<InternalActionResult<decimal>> GetCashOutAsync();
        Task<InternalActionResult<decimal>> GetCurrentBalanceAsync();
    }
}
