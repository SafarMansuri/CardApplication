using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IStockItemRepository
    {
        Task<PaginatedResult<StockItem>> GetAllAsync(PaginationParams pagination);
        Task<StockItem?> GetByIdAsync(int id);
        Task<StockItem> CreateAsync(StockItem model);
        Task<StockItem> UpdateAsync(StockItem model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<PaginatedResult<StockItem>> GetByPartyNameAsync(string partyName, PaginationParams pagination);
        Task<PaginatedResult<StockItem>> GetByStatusAsync(string status, PaginationParams pagination);
        Task<PaginatedResult<StockItem>> GetByInOutAsync(string inOut, PaginationParams pagination);
    }
}
