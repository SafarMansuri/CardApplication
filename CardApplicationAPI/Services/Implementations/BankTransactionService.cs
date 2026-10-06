using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class BankTransactionService : IBankTransactionService
    {
        private readonly IBankTransactionRepository _repository;

        public BankTransactionService(IBankTransactionRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<PaginatedResult<BankTransaction>>> GetAllAsync(PaginationParams pagination)
        {
            var result = await _repository.GetAllAsync(pagination);
            return FactoryResponse<PaginatedResult<BankTransaction>>.CreateSuccess(result, "Bank transactions retrieved successfully.");
        }

        public async Task<InternalActionResult<BankTransaction?>> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return FactoryResponse<BankTransaction?>.CreateFail("Bank transaction not found.");

            return FactoryResponse<BankTransaction?>.CreateSuccess(item, "Bank transaction retrieved successfully.");
        }

        public async Task<InternalActionResult<IEnumerable<BankTransaction>>> GetByAccountIdAsync(int accountId)
        {
            if (accountId <= 0)
                return FactoryResponse<IEnumerable<BankTransaction>>.CreateFail("Account id is invalid.", ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.GetByAccountIdAsync(accountId);
            return FactoryResponse<IEnumerable<BankTransaction>>.CreateSuccess(result, "Bank transactions retrieved by account id.");
        }

        public async Task<InternalActionResult<BankTransaction>> AddAsync(BankTransaction model)
        {
            if (model.AccountId <= 0)
                return FactoryResponse<BankTransaction>.CreateFail("Account id is required.");

            var normalizedType = model.TransactionType?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedType))
                return FactoryResponse<BankTransaction>.CreateFail("Transaction type is required.");

            normalizedType = normalizedType.ToUpperInvariant();
            if (normalizedType != "OPENING" && normalizedType != "IN" && normalizedType != "OUT")
                return FactoryResponse<BankTransaction>.CreateFail("Transaction type must be OPENING, IN, or OUT. Use the transfer endpoint for account transfers.");

            if (model.Amount <= 0)
                return FactoryResponse<BankTransaction>.CreateFail("Amount must be greater than zero.");

            model.TransactionType = normalizedType;
            var created = await _repository.CreateAsync(model);
            return FactoryResponse<BankTransaction>.CreateSuccess(created, "Bank transaction added successfully.");
        }

        public async Task<InternalActionResult<BankTransferResult>> TransferAsync(BankTransferRequest request)
        {
            if (request.FromAccountId == request.ToAccountId)
                return FactoryResponse<BankTransferResult>.CreateFail("Source and destination accounts must be different.", ApplicationActionStatusCodes.ValidationFailed);

            if (request.Amount <= 0)
                return FactoryResponse<BankTransferResult>.CreateFail("Amount must be greater than zero.", ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.TransferAsync(request);
            if (result == null)
                return FactoryResponse<BankTransferResult>.CreateFail("Source or destination bank account was not found.", ApplicationActionStatusCodes.NotFound);

            return FactoryResponse<BankTransferResult>.CreateSuccess(result, "Bank transfer completed successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(BankTransaction model)
        {
            if (model.Id <= 0)
                return FactoryResponse<bool>.CreateFail("Bank transaction id is invalid.");

            if (model.AccountId <= 0)
                return FactoryResponse<bool>.CreateFail("Account id is required.");

            var normalizedType = model.TransactionType?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedType))
                return FactoryResponse<bool>.CreateFail("Transaction type is required.");

            normalizedType = normalizedType.ToUpperInvariant();
            if (normalizedType != "OPENING" && normalizedType != "IN" && normalizedType != "OUT")
                return FactoryResponse<bool>.CreateFail("Transaction type must be OPENING, IN, or OUT. Use the transfer endpoint for account transfers.");

            if (model.Amount <= 0)
                return FactoryResponse<bool>.CreateFail("Amount must be greater than zero.");

            model.TransactionType = normalizedType;
            await _repository.UpdateAsync(model);
            return FactoryResponse<bool>.CreateSuccess(true, "Bank transaction updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return FactoryResponse<bool>.CreateFail("Bank transaction not found.");

            return FactoryResponse<bool>.CreateSuccess(true, "Bank transaction deleted successfully.");
        }
    }
}
