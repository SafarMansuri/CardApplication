using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IStockPaymentInfoService
    {
        Task<InternalActionResult<PaginatedResult<StockPaymentInfo>>> GetAllAsync(PaginationParams pagination, string? partyName, string? inOut, string? paymentType, string? status);
        Task<InternalActionResult<StockPaymentInfo?>> GetByIdAsync(int id);
        Task<InternalActionResult<PaginatedResult<StockPaymentInfo>>> GetByStockItemIdAsync(int stockItemId, PaginationParams pagination);
        Task<InternalActionResult<StockPaymentInfo>> SettleAsync(int id, StockPaymentSettlementRequest request, int userId, string username);
        Task<InternalActionResult<StockPaymentInfo>> ResetAsync(int id, StockPaymentResetRequest request, int userId, string username);
        Task<InternalActionResult<PaginatedResult<StockPaymentHistory>>> GetHistoryAsync(int id, PaginationParams pagination);
        Task<InternalActionResult<PaginatedResult<StockPartyPaymentSummary>>> GetPartySummariesAsync(PaginationParams pagination, string? partyName, string? paymentType, string? status);
        Task<InternalActionResult<StockPaymentTotals>> GetTotalsAsync();
    }
}
