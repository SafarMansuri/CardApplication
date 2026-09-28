using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IStockItemService
    {
        Task<InternalActionResult<PaginatedResult<StockItem>>> GetAllAsync(PaginationParams pagination);
        Task<InternalActionResult<StockItem?>> GetByIdAsync(int id);
        Task<InternalActionResult<StockItem>> AddAsync(StockItem model);
        Task<InternalActionResult<bool>> UpdateAsync(StockItem model);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
        Task<InternalActionResult<PaginatedResult<StockItem>>> GetByPartyNameAsync(string partyName, PaginationParams pagination);
        Task<InternalActionResult<PaginatedResult<StockItem>>> GetByStatusAsync(string status, PaginationParams pagination);
        Task<InternalActionResult<PaginatedResult<StockItem>>> GetByInOutAsync(string inOut, PaginationParams pagination);
    }
}
