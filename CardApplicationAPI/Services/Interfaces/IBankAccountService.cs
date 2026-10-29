using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IBankAccountService
    {
        Task<InternalActionResult<PaginatedResult<BankAccount>>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            bool? isActive = null,
            string? sortBy = null,
            string? sortDir = null);
        Task<InternalActionResult<BankAccount?>> GetByIdAsync(int id);
        Task<InternalActionResult<BankAccount>> AddAsync(BankAccount model);
        Task<InternalActionResult<bool>> UpdateAsync(BankAccount model);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
        Task<InternalActionResult<decimal>> GetTotalBalanceAsync();
        Task<InternalActionResult<PaginatedResult<BankAccountBalanceSummary>>> GetBalanceSummaryAsync(PaginationParams pagination);
        Task<InternalActionResult<BankAccountStatusSummary>> GetStatusSummaryAsync();
    }
}
