using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IStockItemService
    {
        Task<InternalActionResult<IEnumerable<StockItem>>> GetAllAsync();
        Task<InternalActionResult<StockItem?>> GetByIdAsync(int id);
        Task<InternalActionResult<StockItem>> AddAsync(StockItem model);
        Task<InternalActionResult<bool>> UpdateAsync(StockItem model);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
        Task<InternalActionResult<IEnumerable<StockItem>>> GetByPartyNameAsync(string partyName);
        Task<InternalActionResult<IEnumerable<StockItem>>> GetByStatusAsync(string status);
        Task<InternalActionResult<IEnumerable<StockItem>>> GetByInOutAsync(string inOut);
    }
}
