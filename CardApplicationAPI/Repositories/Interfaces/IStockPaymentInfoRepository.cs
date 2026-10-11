using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IStockPaymentInfoRepository
    {
        Task<PaginatedResult<StockPaymentInfo>> GetAllAsync(PaginationParams pagination, string? partyName, string? inOut, string? paymentType, string? status);
        Task<StockPaymentInfo?> GetByIdAsync(int id);
        Task<PaginatedResult<StockPaymentInfo>> GetByStockItemIdAsync(int stockItemId, PaginationParams pagination);
        Task<StockPaymentInfo?> SettleAsync(int id, StockPaymentSettlementRequest request, int userId, string username);
        Task<StockPaymentInfo?> ResetAsync(int id, StockPaymentResetRequest request, int userId, string username);
        Task<PaginatedResult<StockPaymentHistory>> GetHistoryAsync(int id, PaginationParams pagination);
        Task<PaginatedResult<StockPartyPaymentSummary>> GetPartySummariesAsync(PaginationParams pagination, string? partyName, string? paymentType, string? status);
        Task<StockPaymentTotals> GetTotalsAsync();
    }
}
