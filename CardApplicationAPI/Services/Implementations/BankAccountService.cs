using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
                return FactoryResponse<BankAccount?>.CreateFail("Bank account not found.", ApplicationActionStatusCodes.NotFound);

            return FactoryResponse<BankAccount?>.CreateSuccess(item, "Bank account retrieved successfully.");
        }

        public async Task<InternalActionResult<BankAccount>> AddAsync(BankAccount model)
        {
            if (string.IsNullOrWhiteSpace(model.AccountName))
                return FactoryResponse<BankAccount>.CreateFail("Account name is required.", ApplicationActionStatusCodes.ValidationFailed);

            if (string.IsNullOrWhiteSpace(model.BankName))
                return FactoryResponse<BankAccount>.CreateFail("Bank name is required.", ApplicationActionStatusCodes.ValidationFailed);

            model.AccountName = model.AccountName.Trim();
            model.BankName = model.BankName.Trim();

            try
            {
                var created = await _repository.CreateAsync(model);
                return FactoryResponse<BankAccount>.CreateSuccess(created, "Bank account created successfully.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                return FactoryResponse<BankAccount>.CreateFail("Account name is already in use.", ApplicationActionStatusCodes.Conflict);
            }
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(BankAccount model)
        {
            if (model.Id <= 0)
                return FactoryResponse<bool>.CreateFail("Bank account id is invalid.", ApplicationActionStatusCodes.ValidationFailed);

            if (string.IsNullOrWhiteSpace(model.AccountName))
                return FactoryResponse<bool>.CreateFail("Account name is required.", ApplicationActionStatusCodes.ValidationFailed);

            if (string.IsNullOrWhiteSpace(model.BankName))
                return FactoryResponse<bool>.CreateFail("Bank name is required.", ApplicationActionStatusCodes.ValidationFailed);

            model.AccountName = model.AccountName.Trim();
            model.BankName = model.BankName.Trim();

            try
            {
                await _repository.UpdateAsync(model);
                return FactoryResponse<bool>.CreateSuccess(true, "Bank account updated successfully.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                return FactoryResponse<bool>.CreateFail("Account name is already in use.", ApplicationActionStatusCodes.Conflict);
            }
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return FactoryResponse<bool>.CreateFail("Bank account not found.", ApplicationActionStatusCodes.NotFound);

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
