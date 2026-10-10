using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class CashLedgerService : ICashLedgerService
    {
        private readonly ICashLedgerRepository _repository;

        public CashLedgerService(ICashLedgerRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<PaginatedResult<CashLedger>>> GetAllAsync(PaginationParams pagination)
        {
            var result = await _repository.GetAllAsync(pagination);
            return FactoryResponse<PaginatedResult<CashLedger>>.CreateSuccess(result, "Cash ledger retrieved successfully.");
        }

        public async Task<InternalActionResult<CashLedger?>> GetByIdAsync(long id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return FactoryResponse<CashLedger?>.CreateFail("Cash ledger record not found.");

            return FactoryResponse<CashLedger?>.CreateSuccess(item, "Cash ledger record retrieved successfully.");
        }

        public async Task<InternalActionResult<PaginatedResult<CashLedger>>> GetByTypeAsync(string transactionType, PaginationParams pagination)
        {
            if (string.IsNullOrWhiteSpace(transactionType))
                return FactoryResponse<PaginatedResult<CashLedger>>.CreateFail("Transaction type is required.");

            var result = await _repository.GetByTypeAsync(transactionType, pagination);
            return FactoryResponse<PaginatedResult<CashLedger>>.CreateSuccess(result, "Cash ledger filtered by type.");
        }

        public async Task<InternalActionResult<CashLedger>> AddAsync(CashLedger model)
        {
            if (string.IsNullOrWhiteSpace(model.TransactionType))
                return FactoryResponse<CashLedger>.CreateFail("Transaction type is required.");

            var normalized = model.TransactionType.Trim().ToUpperInvariant();
            if (normalized != "OPENING" && normalized != "IN" && normalized != "OUT")
                return FactoryResponse<CashLedger>.CreateFail("Transaction type must be OPENING, IN, or OUT.");

            if (model.Amount <= 0)
                return FactoryResponse<CashLedger>.CreateFail("Amount must be greater than zero.");

            model.TransactionType = normalized;
            var created = await _repository.CreateAsync(model);
            return FactoryResponse<CashLedger>.CreateSuccess(created, "Cash ledger entry added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(CashLedger model)
        {
            if (model.Id <= 0)
                return FactoryResponse<bool>.CreateFail("Cash ledger id is invalid.");

            if (string.IsNullOrWhiteSpace(model.TransactionType))
                return FactoryResponse<bool>.CreateFail("Transaction type is required.");

            var normalized = model.TransactionType.Trim().ToUpperInvariant();
            if (normalized != "OPENING" && normalized != "IN" && normalized != "OUT")
                return FactoryResponse<bool>.CreateFail("Transaction type must be OPENING, IN, or OUT.");

            if (model.Amount <= 0)
                return FactoryResponse<bool>.CreateFail("Amount must be greater than zero.");

            model.TransactionType = normalized;
            await _repository.UpdateAsync(model);
            return FactoryResponse<bool>.CreateSuccess(true, "Cash ledger entry updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(long id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return FactoryResponse<bool>.CreateFail("Cash ledger record not found.");

            return FactoryResponse<bool>.CreateSuccess(true, "Cash ledger entry deleted successfully.");
        }

        public async Task<InternalActionResult<decimal>> GetOpeningBalanceAsync()
        {
            var result = await _repository.GetOpeningBalanceAsync();
            return FactoryResponse<decimal>.CreateSuccess(result, "Opening balance retrieved successfully.");
        }

        public async Task<InternalActionResult<decimal>> GetCashInAsync()
        {
            var result = await _repository.GetCashInAsync();
            return FactoryResponse<decimal>.CreateSuccess(result, "Cash in total retrieved successfully.");
        }

        public async Task<InternalActionResult<decimal>> GetCashOutAsync()
        {
            var result = await _repository.GetCashOutAsync();
            return FactoryResponse<decimal>.CreateSuccess(result, "Cash out total retrieved successfully.");
        }

        public async Task<InternalActionResult<decimal>> GetCurrentBalanceAsync()
        {
            var result = await _repository.GetCurrentBalanceAsync();
            return FactoryResponse<decimal>.CreateSuccess(result, "Current cash balance retrieved successfully.");
        }
    }
}
