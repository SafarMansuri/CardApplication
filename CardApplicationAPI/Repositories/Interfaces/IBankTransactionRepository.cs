using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IBankTransactionRepository
    {
        Task<PaginatedResult<BankTransaction>> GetAllAsync(PaginationParams pagination);
        Task<BankTransaction?> GetByIdAsync(int id);
        Task<IEnumerable<BankTransaction>> GetByAccountIdAsync(int accountId);
        Task<BankTransaction> CreateAsync(BankTransaction model);
        Task<BankTransferResult?> TransferAsync(BankTransferRequest request);
        Task<BankTransaction> UpdateAsync(BankTransaction model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
