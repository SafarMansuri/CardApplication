using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IBankTransactionService
    {
        Task<InternalActionResult<IEnumerable<BankTransaction>>> GetAllAsync();
        Task<InternalActionResult<BankTransaction?>> GetByIdAsync(int id);
        Task<InternalActionResult<IEnumerable<BankTransaction>>> GetByAccountIdAsync(int accountId);
        Task<InternalActionResult<BankTransaction>> AddAsync(BankTransaction model);
        Task<InternalActionResult<bool>> UpdateAsync(BankTransaction model);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
    }
}
