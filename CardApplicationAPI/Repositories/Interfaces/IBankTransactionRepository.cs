using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IBankTransactionRepository
    {
        Task<PaginatedResult<BankTransaction>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            IReadOnlyCollection<string>? transactionTypes = null,
            string? sortBy = null,
            string? sortDir = null);
        Task<BankTransaction?> GetByIdAsync(int id);
        Task<PaginatedResult<BankTransaction>> GetByAccountIdAsync(
            int accountId,
            PaginationParams pagination,
            string? search = null,
            IReadOnlyCollection<string>? transactionTypes = null,
            string? sortBy = null,
            string? sortDir = null);
        Task<BankTransaction> CreateAsync(BankTransaction model);
        Task<BankTransferResult> TransferAsync(BankTransferRequest request);
        Task<BankTransaction> UpdateAsync(BankTransaction model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
