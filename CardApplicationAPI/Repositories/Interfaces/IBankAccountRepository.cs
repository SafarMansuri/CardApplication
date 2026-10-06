using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IBankAccountRepository
    {
        Task<PaginatedResult<BankAccount>> GetAllAsync(PaginationParams pagination);
        Task<BankAccount?> GetByIdAsync(int id);
        Task<BankAccount> CreateAsync(BankAccount model);
        Task<BankAccount> UpdateAsync(BankAccount model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<decimal> GetTotalBalanceAsync();
        Task<IEnumerable<BankAccountBalanceSummary>> GetAccountBalanceSummaryAsync();
    }
}

public class BankAccountBalanceSummary
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal TransferIn { get; set; }
    public decimal TransferOut { get; set; }
    public decimal CurrentBalance { get; set; }
}
