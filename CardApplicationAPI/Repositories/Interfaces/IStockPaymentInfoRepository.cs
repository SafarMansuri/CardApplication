using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IStockPaymentInfoRepository
    {
        Task<PaginatedResult<StockPaymentInfo>> GetAllAsync(PaginationParams pagination);
        Task<StockPaymentInfo?> GetByIdAsync(int id);
        Task<PaginatedResult<StockPaymentInfo>> GetByStockItemIdAsync(int stockItemId, PaginationParams pagination);
        Task<StockPaymentInfo> CreateAsync(StockPaymentInfo model);
        Task<StockPaymentInfo> UpdateAsync(StockPaymentInfo model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
