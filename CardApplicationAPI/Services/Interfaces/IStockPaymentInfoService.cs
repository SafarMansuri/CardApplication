using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IStockPaymentInfoService
    {
        Task<InternalActionResult<PaginatedResult<StockPaymentInfo>>> GetAllAsync(PaginationParams pagination);
        Task<InternalActionResult<StockPaymentInfo?>> GetByIdAsync(int id);
        Task<InternalActionResult<PaginatedResult<StockPaymentInfo>>> GetByStockItemIdAsync(int stockItemId, PaginationParams pagination);
        Task<InternalActionResult<StockPaymentInfo>> AddAsync(StockPaymentInfo model);
        Task<InternalActionResult<bool>> UpdateAsync(StockPaymentInfo model);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
    }
}
