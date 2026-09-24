using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class StockPaymentInfoService : IStockPaymentInfoService
    {
        private readonly IStockPaymentInfoRepository _repository;

        public StockPaymentInfoService(IStockPaymentInfoRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<IEnumerable<StockPaymentInfo>>> GetAllAsync()
        {
            var result = await _repository.GetAllAsync();
            return FactoryResponse<IEnumerable<StockPaymentInfo>>.CreateSuccess(result, "Stock payment records retrieved successfully.");
        }

        public async Task<InternalActionResult<StockPaymentInfo?>> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return FactoryResponse<StockPaymentInfo?>.CreateFail("Stock payment record not found.");

            return FactoryResponse<StockPaymentInfo?>.CreateSuccess(item, "Stock payment record retrieved successfully.");
        }

        public async Task<InternalActionResult<IEnumerable<StockPaymentInfo>>> GetByStockItemIdAsync(int stockItemId)
        {
            if (stockItemId <= 0)
                return FactoryResponse<IEnumerable<StockPaymentInfo>>.CreateFail("Stock item id is invalid.");

            var result = await _repository.GetByStockItemIdAsync(stockItemId);
            return FactoryResponse<IEnumerable<StockPaymentInfo>>.CreateSuccess(result, "Payments for stock item retrieved successfully.");
        }

        public async Task<InternalActionResult<StockPaymentInfo>> AddAsync(StockPaymentInfo model)
        {
            if (string.IsNullOrWhiteSpace(model.No))
                return FactoryResponse<StockPaymentInfo>.CreateFail("Payment number is required.");

            if (model.StockItemId <= 0)
                return FactoryResponse<StockPaymentInfo>.CreateFail("Stock item id is required.");

            var created = await _repository.CreateAsync(model);
            return FactoryResponse<StockPaymentInfo>.CreateSuccess(created, "Stock payment record added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(StockPaymentInfo model)
        {
            if (model.Id <= 0)
                return FactoryResponse<bool>.CreateFail("Stock payment id is invalid.");

            await _repository.UpdateAsync(model);
            return FactoryResponse<bool>.CreateSuccess(true, "Stock payment record updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return FactoryResponse<bool>.CreateFail("Stock payment record not found.");

            return FactoryResponse<bool>.CreateSuccess(true, "Stock payment record deleted successfully.");
        }
    }
}
