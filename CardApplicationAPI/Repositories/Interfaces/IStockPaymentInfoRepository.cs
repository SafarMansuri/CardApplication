using CardApplicationAPI.Models;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IStockPaymentInfoRepository
    {
        Task<IEnumerable<StockPaymentInfo>> GetAllAsync();
        Task<StockPaymentInfo?> GetByIdAsync(int id);
        Task<IEnumerable<StockPaymentInfo>> GetByStockItemIdAsync(int stockItemId);
        Task<StockPaymentInfo> CreateAsync(StockPaymentInfo model);
        Task<StockPaymentInfo> UpdateAsync(StockPaymentInfo model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
