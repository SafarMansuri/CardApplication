using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IStockPaymentInfoService
    {
        Task<InternalActionResult<IEnumerable<StockPaymentInfo>>> GetAllAsync();
        Task<InternalActionResult<StockPaymentInfo?>> GetByIdAsync(int id);
        Task<InternalActionResult<IEnumerable<StockPaymentInfo>>> GetByStockItemIdAsync(int stockItemId);
        Task<InternalActionResult<StockPaymentInfo>> AddAsync(StockPaymentInfo model);
        Task<InternalActionResult<bool>> UpdateAsync(StockPaymentInfo model);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
    }
}
