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

        public async Task<InternalActionResult<PaginatedResult<StockItem>>> GetAllAsync(PaginationParams pagination) =>
            FactoryResponse<PaginatedResult<StockItem>>.CreateSuccess(await _repository.GetAllAsync(pagination), "Stock items retrieved successfully.");

        public async Task<InternalActionResult<StockItem?>> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item == null
                ? FactoryResponse<StockItem?>.CreateFail("Stock item not found.", ApplicationActionStatusCodes.NotFound)
                : FactoryResponse<StockItem?>.CreateSuccess(item, "Stock item retrieved successfully.");
        }

        public async Task<InternalActionResult<StockItem>> AddAsync(StockItem model)
        {
            var validation = NormalizeAndValidate(model);
            if (validation != null)
                return FactoryResponse<StockItem>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            return FactoryResponse<StockItem>.CreateSuccess(await _repository.CreateAsync(model), "Stock item added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(StockItem model)
        {
            if (model.Id <= 0)
                return FactoryResponse<bool>.CreateFail("Stock item id is invalid.", ApplicationActionStatusCodes.ValidationFailed);
            var validation = NormalizeAndValidate(model);
            if (validation != null)
                return FactoryResponse<bool>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            try
            {
                await _repository.UpdateAsync(model);
                return FactoryResponse<bool>.CreateSuccess(true, "Stock item updated successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return FactoryResponse<bool>.CreateFail(ex.Message, ex.Message.StartsWith("Reset ") ? ApplicationActionStatusCodes.Conflict : ApplicationActionStatusCodes.NotFound);
            }
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            try
            {
                return await _repository.DeleteAsync(id)
                    ? FactoryResponse<bool>.CreateSuccess(true, "Stock item deleted successfully.")
                    : FactoryResponse<bool>.CreateFail("Stock item not found.", ApplicationActionStatusCodes.NotFound);
            }
            catch (InvalidOperationException ex)
            {
                return FactoryResponse<bool>.CreateFail(ex.Message, ApplicationActionStatusCodes.Conflict);
            }
        }

        public async Task<InternalActionResult<PaginatedResult<StockItem>>> GetByPartyNameAsync(string partyName, PaginationParams pagination)
        {
            if (string.IsNullOrWhiteSpace(partyName))
                return FactoryResponse<PaginatedResult<StockItem>>.CreateFail("Party name is required.", ApplicationActionStatusCodes.ValidationFailed);
            return FactoryResponse<PaginatedResult<StockItem>>.CreateSuccess(await _repository.GetByPartyNameAsync(partyName, pagination), "Stock items retrieved by party name.");
        }

        public async Task<InternalActionResult<PaginatedResult<StockItem>>> GetByStatusAsync(string status, PaginationParams pagination)
        {
            if (string.IsNullOrWhiteSpace(status))
                return FactoryResponse<PaginatedResult<StockItem>>.CreateFail("Status is required.", ApplicationActionStatusCodes.ValidationFailed);
            return FactoryResponse<PaginatedResult<StockItem>>.CreateSuccess(await _repository.GetByStatusAsync(status, pagination), "Stock items retrieved by status.");
        }

        public async Task<InternalActionResult<PaginatedResult<StockItem>>> GetByInOutAsync(string inOut, PaginationParams pagination)
        {
            if (string.IsNullOrWhiteSpace(inOut))
                return FactoryResponse<PaginatedResult<StockItem>>.CreateFail("In/Out value is required.", ApplicationActionStatusCodes.ValidationFailed);
            return FactoryResponse<PaginatedResult<StockItem>>.CreateSuccess(await _repository.GetByInOutAsync(inOut, pagination), "Stock items retrieved by In/Out filter.");
        }

        private static string? NormalizeAndValidate(StockItem model)
        {
            if (string.IsNullOrWhiteSpace(model.No))
                return "Stock number is required.";
            model.InOut = model.InOut?.Trim().ToUpperInvariant();
            if (model.InOut != "IN" && model.InOut != "OUT")
                return "InOut must be IN or OUT.";
            if (model.Amount is null or <= 0)
                return "Amount must be greater than zero.";
            if (model.InOut == "IN" && string.IsNullOrWhiteSpace(model.PartyName))
                return "PartyName is required for IN stock.";
            if (model.InOut == "OUT" && string.IsNullOrWhiteSpace(model.SoldTo))
                return "SoldTo is required for OUT stock.";
            model.PartyName = model.PartyName?.Trim();
            model.SoldTo = model.SoldTo?.Trim();
            if (!string.IsNullOrWhiteSpace(model.Status))
                model.Status = char.ToUpperInvariant(model.Status.Trim()[0]) + model.Status.Trim()[1..].ToLowerInvariant();
            return null;
        }
    }
}
