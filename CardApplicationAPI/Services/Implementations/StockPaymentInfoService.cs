using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class StockPaymentInfoService : IStockPaymentInfoService
    {
        private readonly IStockPaymentInfoRepository _repository;

        public StockPaymentInfoService(IStockPaymentInfoRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<PaginatedResult<StockPaymentInfo>>> GetAllAsync(PaginationParams pagination, string? partyName, string? inOut, string? paymentType, string? status)
        {
            var validation = NormalizeFilters(ref inOut, ref paymentType, ref status);
            if (validation != null) return FactoryResponse<PaginatedResult<StockPaymentInfo>>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            return FactoryResponse<PaginatedResult<StockPaymentInfo>>.CreateSuccess(await _repository.GetAllAsync(pagination, partyName, inOut, paymentType, status), "Stock payment records retrieved successfully.");
        }

        public async Task<InternalActionResult<StockPaymentInfo?>> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null
                ? FactoryResponse<StockPaymentInfo?>.CreateFail("Stock payment record not found.", ApplicationActionStatusCodes.NotFound)
                : FactoryResponse<StockPaymentInfo?>.CreateSuccess(item, "Stock payment record retrieved successfully.");
        }

        public async Task<InternalActionResult<PaginatedResult<StockPaymentInfo>>> GetByStockItemIdAsync(int stockItemId, PaginationParams pagination)
        {
            if (stockItemId <= 0) return FactoryResponse<PaginatedResult<StockPaymentInfo>>.CreateFail("Stock item id is invalid.", ApplicationActionStatusCodes.ValidationFailed);
            return FactoryResponse<PaginatedResult<StockPaymentInfo>>.CreateSuccess(await _repository.GetByStockItemIdAsync(stockItemId, pagination), "Payments for stock item retrieved successfully.");
        }

        public async Task<InternalActionResult<StockPaymentInfo>> SettleAsync(int id, StockPaymentSettlementRequest request, int userId, string username)
        {
            if (request.Amount <= 0 || request.TransactionDate == default || string.IsNullOrWhiteSpace(request.PaymentMethod))
                return FactoryResponse<StockPaymentInfo>.CreateFail("A positive amount, transaction date, and payment method are required.", ApplicationActionStatusCodes.ValidationFailed);
            try
            {
                var result = await _repository.SettleAsync(id, request, userId, username);
                return result == null
                    ? FactoryResponse<StockPaymentInfo>.CreateFail("Stock payment record not found.", ApplicationActionStatusCodes.NotFound)
                    : FactoryResponse<StockPaymentInfo>.CreateSuccess(result, "Stock payment completed successfully.");
            }
            catch (ArgumentException ex) { return FactoryResponse<StockPaymentInfo>.CreateFail(ex.Message, ApplicationActionStatusCodes.ValidationFailed); }
            catch (InvalidOperationException ex) { return FactoryResponse<StockPaymentInfo>.CreateFail(ex.Message, ApplicationActionStatusCodes.Conflict); }
        }

        public async Task<InternalActionResult<StockPaymentInfo>> ResetAsync(int id, StockPaymentResetRequest request, int userId, string username)
        {
            if (string.IsNullOrWhiteSpace(request.Reason)) return FactoryResponse<StockPaymentInfo>.CreateFail("Reset reason is required.", ApplicationActionStatusCodes.ValidationFailed);
            try
            {
                var result = await _repository.ResetAsync(id, request, userId, username);
                return result == null
                    ? FactoryResponse<StockPaymentInfo>.CreateFail("Stock payment record not found.", ApplicationActionStatusCodes.NotFound)
                    : FactoryResponse<StockPaymentInfo>.CreateSuccess(result, "Stock payment reset successfully.");
            }
            catch (InvalidOperationException ex) { return FactoryResponse<StockPaymentInfo>.CreateFail(ex.Message, ApplicationActionStatusCodes.Conflict); }
        }

        public async Task<InternalActionResult<PaginatedResult<StockPaymentHistory>>> GetHistoryAsync(int id, PaginationParams pagination)
        {
            if (await _repository.GetByIdAsync(id) == null) return FactoryResponse<PaginatedResult<StockPaymentHistory>>.CreateFail("Stock payment record not found.", ApplicationActionStatusCodes.NotFound);
            return FactoryResponse<PaginatedResult<StockPaymentHistory>>.CreateSuccess(await _repository.GetHistoryAsync(id, pagination), "Stock payment history retrieved successfully.");
        }

        public async Task<InternalActionResult<PaginatedResult<StockPartyPaymentSummary>>> GetPartySummariesAsync(PaginationParams pagination, string? partyName, string? paymentType, string? status)
        {
            string? inOut = null;
            var validation = NormalizeFilters(ref inOut, ref paymentType, ref status);
            if (validation != null) return FactoryResponse<PaginatedResult<StockPartyPaymentSummary>>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            return FactoryResponse<PaginatedResult<StockPartyPaymentSummary>>.CreateSuccess(await _repository.GetPartySummariesAsync(pagination, partyName, paymentType, status), "Stock party payment summaries retrieved successfully.");
        }

        public async Task<InternalActionResult<StockPaymentTotals>> GetTotalsAsync() =>
            FactoryResponse<StockPaymentTotals>.CreateSuccess(await _repository.GetTotalsAsync(), "Stock payment totals retrieved successfully.");

        private static string? NormalizeFilters(ref string? inOut, ref string? paymentType, ref string? status)
        {
            inOut = inOut?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(inOut) && inOut != "IN" && inOut != "OUT") return "InOut must be IN or OUT.";
            paymentType = NormalizeTitle(paymentType);
            if (!string.IsNullOrWhiteSpace(paymentType) && paymentType != "Payable" && paymentType != "Receivable") return "PaymentType must be Payable or Receivable.";
            status = NormalizeTitle(status);
            if (!string.IsNullOrWhiteSpace(status) && status != "Pending" && status != "Completed") return "Status must be Pending or Completed.";
            return null;
        }

        private static string? NormalizeTitle(string? value) => string.IsNullOrWhiteSpace(value) ? null : char.ToUpperInvariant(value.Trim()[0]) + value.Trim()[1..].ToLowerInvariant();
    }
}
