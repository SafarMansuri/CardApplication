using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class CardInfoService : ICardInfoService
    {
        private readonly ICardInfoRepository _repository;

        public CardInfoService(ICardInfoRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<PaginatedResult<CardInfo>>> GetAllAsync(PaginationParams pagination)
        {
            var result = await _repository.GetAllAsync(pagination);
            return FactoryResponse<PaginatedResult<CardInfo>>.CreateSuccess(result, "Card info retrieved successfully.");
        }

        public async Task<InternalActionResult<CardInfo?>> GetByIdAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
                return FactoryResponse<CardInfo?>.CreateFail("Card info not found.", ApplicationActionStatusCodes.NotFound);

            return FactoryResponse<CardInfo?>.CreateSuccess(user, "User retrieved successfully.");
        }

        public async Task<InternalActionResult<CardInfo>> AddAsync(CardInfo user)
        {
            var validation = Validate(user);
            if (validation != null)
                return FactoryResponse<CardInfo>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            var created = await _repository.CreateAsync(user);
            return FactoryResponse<CardInfo>.CreateSuccess(created, "User added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(CardInfo user)
        {
            var validation = Validate(user);
            if (validation != null)
                return FactoryResponse<bool>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            if (!await _repository.ExistsAsync(user.Id))
                return FactoryResponse<bool>.CreateFail("Card info not found.", ApplicationActionStatusCodes.NotFound);
            await _repository.UpdateAsync(user);
            return FactoryResponse<bool>.CreateSuccess(true, "User updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            if (!await _repository.DeleteAsync(id))
                return FactoryResponse<bool>.CreateFail("Card info not found.", ApplicationActionStatusCodes.NotFound);
            return FactoryResponse<bool>.CreateSuccess(true, "User deleted successfully.");
        }

        private static string? Validate(CardInfo model)
        {
            if (string.IsNullOrWhiteSpace(model.CardName) || string.IsNullOrWhiteSpace(model.CardNumber) || string.IsNullOrWhiteSpace(model.PartyName))
                return "CardName, CardNumber, and PartyName are required.";
            if (model.Deduction < 0 || model.Profit < 0)
                return "Deduction and Profit cannot be negative.";
            return null;
        }
    }
}
