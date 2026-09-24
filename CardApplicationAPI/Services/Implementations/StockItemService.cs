using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class StockItemService : IStockItemService
    {
        private readonly IStockItemRepository _repository;

        public StockItemService(IStockItemRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<IEnumerable<StockItem>>> GetAllAsync()
        {
            var result = await _repository.GetAllAsync();
            return FactoryResponse<IEnumerable<StockItem>>.CreateSuccess(result, "Stock items retrieved successfully.");
        }

        public async Task<InternalActionResult<StockItem?>> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return FactoryResponse<StockItem?>.CreateFail("Stock item not found.");

            return FactoryResponse<StockItem?>.CreateSuccess(item, "Stock item retrieved successfully.");
        }

        public async Task<InternalActionResult<StockItem>> AddAsync(StockItem model)
        {
            if (string.IsNullOrWhiteSpace(model.No))
                return FactoryResponse<StockItem>.CreateFail("Stock number is required.");

            var created = await _repository.CreateAsync(model);
            return FactoryResponse<StockItem>.CreateSuccess(created, "Stock item added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(StockItem model)
        {
            if (model.Id <= 0)
                return FactoryResponse<bool>.CreateFail("Stock item id is invalid.");

            await _repository.UpdateAsync(model);
            return FactoryResponse<bool>.CreateSuccess(true, "Stock item updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return FactoryResponse<bool>.CreateFail("Stock item not found.");

            return FactoryResponse<bool>.CreateSuccess(true, "Stock item deleted successfully.");
        }

        public async Task<InternalActionResult<IEnumerable<StockItem>>> GetByPartyNameAsync(string partyName)
        {
            if (string.IsNullOrWhiteSpace(partyName))
                return FactoryResponse<IEnumerable<StockItem>>.CreateFail("Party name is required.");

            var result = await _repository.GetByPartyNameAsync(partyName);
            return FactoryResponse<IEnumerable<StockItem>>.CreateSuccess(result, "Stock items retrieved by party name.");
        }

        public async Task<InternalActionResult<IEnumerable<StockItem>>> GetByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return FactoryResponse<IEnumerable<StockItem>>.CreateFail("Status is required.");

            var result = await _repository.GetByStatusAsync(status);
            return FactoryResponse<IEnumerable<StockItem>>.CreateSuccess(result, "Stock items retrieved by status.");
        }

        public async Task<InternalActionResult<IEnumerable<StockItem>>> GetByInOutAsync(string inOut)
        {
            if (string.IsNullOrWhiteSpace(inOut))
                return FactoryResponse<IEnumerable<StockItem>>.CreateFail("In/Out value is required.");

            var result = await _repository.GetByInOutAsync(inOut);
            return FactoryResponse<IEnumerable<StockItem>>.CreateSuccess(result, "Stock items retrieved by In/Out filter.");
        }
    }
}
