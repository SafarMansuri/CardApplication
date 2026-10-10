using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class BankAccountService : IBankAccountService
    {
        private readonly IBankAccountRepository _repository;

        public BankAccountService(IBankAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<PaginatedResult<BankAccount>>> GetAllAsync(PaginationParams pagination)
        {
            var result = await _repository.GetAllAsync(pagination);
            return FactoryResponse<PaginatedResult<BankAccount>>.CreateSuccess(result, "Bank accounts retrieved successfully.");
        }

        public async Task<InternalActionResult<BankAccount?>> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return FactoryResponse<BankAccount?>.CreateFail("Bank account not found.");

            return FactoryResponse<BankAccount?>.CreateSuccess(item, "Bank account retrieved successfully.");
        }

        public async Task<InternalActionResult<BankAccount>> AddAsync(BankAccount model)
        {
            if (string.IsNullOrWhiteSpace(model.AccountName))
                return FactoryResponse<BankAccount>.CreateFail("Account name is required.");

            if (string.IsNullOrWhiteSpace(model.BankName))
                return FactoryResponse<BankAccount>.CreateFail("Bank name is required.");

            model.AccountName = model.AccountName.Trim();
            model.BankName = model.BankName.Trim();

            var created = await _repository.CreateAsync(model);
            return FactoryResponse<BankAccount>.CreateSuccess(created, "Bank account created successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(BankAccount model)
        {
            if (model.Id <= 0)
                return FactoryResponse<bool>.CreateFail("Bank account id is invalid.");

            if (string.IsNullOrWhiteSpace(model.AccountName))
                return FactoryResponse<bool>.CreateFail("Account name is required.");

            if (string.IsNullOrWhiteSpace(model.BankName))
                return FactoryResponse<bool>.CreateFail("Bank name is required.");

            model.AccountName = model.AccountName.Trim();
            model.BankName = model.BankName.Trim();

            await _repository.UpdateAsync(model);
            return FactoryResponse<bool>.CreateSuccess(true, "Bank account updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return FactoryResponse<bool>.CreateFail("Bank account not found.");

            return FactoryResponse<bool>.CreateSuccess(true, "Bank account deleted successfully.");
        }

        public async Task<InternalActionResult<decimal>> GetTotalBalanceAsync()
        {
            var result = await _repository.GetTotalBalanceAsync();
            return FactoryResponse<decimal>.CreateSuccess(result, "Total bank balance retrieved successfully.");
        }

        public async Task<InternalActionResult<PaginatedResult<BankAccountBalanceSummary>>> GetBalanceSummaryAsync(PaginationParams pagination)
        {
            var result = await _repository.GetAccountBalanceSummaryAsync(pagination);
            return FactoryResponse<PaginatedResult<BankAccountBalanceSummary>>.CreateSuccess(result, "Bank balances retrieved successfully.");
        }
    }
}
