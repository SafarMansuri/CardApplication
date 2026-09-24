using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface ICashLedgerService
    {
        Task<InternalActionResult<IEnumerable<CashLedger>>> GetAllAsync();
        Task<InternalActionResult<CashLedger?>> GetByIdAsync(long id);
        Task<InternalActionResult<IEnumerable<CashLedger>>> GetByTypeAsync(string transactionType);
        Task<InternalActionResult<CashLedger>> AddAsync(CashLedger model);
        Task<InternalActionResult<bool>> UpdateAsync(CashLedger model);
        Task<InternalActionResult<bool>> DeleteAsync(long id);
        Task<InternalActionResult<decimal>> GetOpeningBalanceAsync();
        Task<InternalActionResult<decimal>> GetCashInAsync();
        Task<InternalActionResult<decimal>> GetCashOutAsync();
        Task<InternalActionResult<decimal>> GetCurrentBalanceAsync();
    }
}
