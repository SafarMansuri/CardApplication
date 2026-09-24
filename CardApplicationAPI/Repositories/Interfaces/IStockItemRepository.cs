using CardApplicationAPI.Models;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IStockItemRepository
    {
        Task<IEnumerable<StockItem>> GetAllAsync();
        Task<StockItem?> GetByIdAsync(int id);
        Task<StockItem> CreateAsync(StockItem model);
        Task<StockItem> UpdateAsync(StockItem model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<IEnumerable<StockItem>> GetByPartyNameAsync(string partyName);
        Task<IEnumerable<StockItem>> GetByStatusAsync(string status);
        Task<IEnumerable<StockItem>> GetByInOutAsync(string inOut);
    }
}
