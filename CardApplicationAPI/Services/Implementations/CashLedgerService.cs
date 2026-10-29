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

        private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "id", "transactionDate", "transactionType", "name", "amount", "remarks", "createdOn", "createdBy"
        };

        public async Task<InternalActionResult<PaginatedResult<CashLedger>>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            string? transactionType = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            string? normalizedType = null;
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                normalizedType = transactionType.Trim().ToUpperInvariant();
                if (normalizedType is not ("OPENING" or "IN" or "OUT"))
                    return FactoryResponse<PaginatedResult<CashLedger>>.CreateFail("Transaction type must be OPENING, IN, or OUT.", ApplicationActionStatusCodes.ValidationFailed);
            }

            sortBy = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.Trim();
            if (sortBy != null && !AllowedSortFields.Contains(sortBy))
                return FactoryResponse<PaginatedResult<CashLedger>>.CreateFail("Invalid sortBy field.", ApplicationActionStatusCodes.ValidationFailed);

            sortDir = string.IsNullOrWhiteSpace(sortDir) ? null : sortDir.Trim().ToLowerInvariant();
            if (sortDir != null && sortDir != "asc" && sortDir != "desc")
                return FactoryResponse<PaginatedResult<CashLedger>>.CreateFail("SortDir must be asc or desc.", ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.GetAllAsync(pagination, search, normalizedType, sortBy, sortDir);
            return FactoryResponse<PaginatedResult<CashLedger>>.CreateSuccess(result, "Cash ledger retrieved successfully.");
        }

        public async Task<InternalActionResult<CashLedger?>> GetByIdAsync(long id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return FactoryResponse<CashLedger?>.CreateFail("Cash ledger record not found.", ApplicationActionStatusCodes.NotFound);

            return FactoryResponse<CashLedger?>.CreateSuccess(item, "Cash ledger record retrieved successfully.");
        }

        public async Task<InternalActionResult<PaginatedResult<CashLedger>>> GetByTypeAsync(string transactionType, PaginationParams pagination)
        {
            if (string.IsNullOrWhiteSpace(transactionType))
                return FactoryResponse<PaginatedResult<CashLedger>>.CreateFail("Transaction type is required.", ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.GetByTypeAsync(transactionType, pagination);
            return FactoryResponse<PaginatedResult<CashLedger>>.CreateSuccess(result, "Cash ledger filtered by type.");
        }

        public async Task<InternalActionResult<CashLedger>> AddAsync(CashLedger model)
        {
            if (string.IsNullOrWhiteSpace(model.TransactionType))
                return FactoryResponse<CashLedger>.CreateFail("Transaction type is required.", ApplicationActionStatusCodes.ValidationFailed);

            var normalized = model.TransactionType.Trim().ToUpperInvariant();
            if (normalized != "OPENING" && normalized != "IN" && normalized != "OUT")
                return FactoryResponse<CashLedger>.CreateFail("Transaction type must be OPENING, IN, or OUT.", ApplicationActionStatusCodes.ValidationFailed);

            if (model.Amount <= 0)
                return FactoryResponse<CashLedger>.CreateFail("Amount must be greater than zero.", ApplicationActionStatusCodes.ValidationFailed);

            model.TransactionType = normalized;
            var created = await _repository.CreateAsync(model);
            return FactoryResponse<CashLedger>.CreateSuccess(created, "Cash ledger entry added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(CashLedger model)
        {
            if (model.Id <= 0)
                return FactoryResponse<bool>.CreateFail("Cash ledger id is invalid.", ApplicationActionStatusCodes.ValidationFailed);

            if (string.IsNullOrWhiteSpace(model.TransactionType))
                return FactoryResponse<bool>.CreateFail("Transaction type is required.", ApplicationActionStatusCodes.ValidationFailed);

            var normalized = model.TransactionType.Trim().ToUpperInvariant();
            if (normalized != "OPENING" && normalized != "IN" && normalized != "OUT")
                return FactoryResponse<bool>.CreateFail("Transaction type must be OPENING, IN, or OUT.", ApplicationActionStatusCodes.ValidationFailed);

            if (model.Amount <= 0)
                return FactoryResponse<bool>.CreateFail("Amount must be greater than zero.", ApplicationActionStatusCodes.ValidationFailed);

            model.TransactionType = normalized;
            await _repository.UpdateAsync(model);
            return FactoryResponse<bool>.CreateSuccess(true, "Cash ledger entry updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(long id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return FactoryResponse<bool>.CreateFail("Cash ledger record not found.", ApplicationActionStatusCodes.NotFound);

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
