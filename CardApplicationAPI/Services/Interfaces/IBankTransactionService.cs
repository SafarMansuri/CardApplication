using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IBankTransactionService
    {
        Task<InternalActionResult<PaginatedResult<BankTransaction>>> GetAllAsync(PaginationParams pagination);
        Task<InternalActionResult<BankTransaction?>> GetByIdAsync(int id);
        Task<InternalActionResult<PaginatedResult<BankTransaction>>> GetByAccountIdAsync(int accountId, PaginationParams pagination);
        Task<InternalActionResult<BankTransaction>> AddAsync(BankTransaction model);
        Task<InternalActionResult<BankTransferResult>> TransferAsync(BankTransferRequest request);
        Task<InternalActionResult<bool>> UpdateAsync(BankTransaction model);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
    }
}
