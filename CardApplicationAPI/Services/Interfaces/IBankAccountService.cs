using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IBankAccountService
    {
        Task<InternalActionResult<IEnumerable<BankAccount>>> GetAllAsync();
        Task<InternalActionResult<BankAccount?>> GetByIdAsync(int id);
        Task<InternalActionResult<BankAccount>> AddAsync(BankAccount model);
        Task<InternalActionResult<bool>> UpdateAsync(BankAccount model);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
        Task<InternalActionResult<decimal>> GetTotalBalanceAsync();
        Task<InternalActionResult<IEnumerable<BankAccountBalanceSummary>>> GetBalanceSummaryAsync();
    }
}
