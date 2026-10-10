using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IBankTransactionRepository
    {
        Task<IEnumerable<BankTransaction>> GetAllAsync();
        Task<BankTransaction?> GetByIdAsync(int id);
        Task<IEnumerable<BankTransaction>> GetByAccountIdAsync(int accountId);
        Task<BankTransaction> CreateAsync(BankTransaction model);
        Task<BankTransaction> UpdateAsync(BankTransaction model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
