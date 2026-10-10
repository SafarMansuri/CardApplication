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

            return FactoryResponse<CardInfo?>.CreateSuccess(user, "Card info retrieved successfully.");
        }

        public async Task<InternalActionResult<CardInfo>> AddAsync(CardInfo user)
        {
            var validation = Validate(user);
            if (validation != null)
                return FactoryResponse<CardInfo>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            var created = await _repository.CreateAsync(user);
            return FactoryResponse<CardInfo>.CreateSuccess(created, "Card info added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(CardInfo user)
        {
            var validation = Validate(user);
            if (validation != null)
                return FactoryResponse<bool>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            var existing = await _repository.GetByIdAsync(user.Id);
            if (existing == null)
                return FactoryResponse<bool>.CreateFail("Card info not found.", ApplicationActionStatusCodes.NotFound);
            if (existing.IsSettled)
                return FactoryResponse<bool>.CreateFail("Settled card info cannot be updated.", ApplicationActionStatusCodes.Conflict);
            try
            {
                await _repository.UpdateAsync(user);
            }
            catch (InvalidOperationException ex)
            {
                return FactoryResponse<bool>.CreateFail(ex.Message, ApplicationActionStatusCodes.Conflict);
            }
            return FactoryResponse<bool>.CreateSuccess(true, "Card info updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return FactoryResponse<bool>.CreateFail("Card info not found.", ApplicationActionStatusCodes.NotFound);
            if (existing.IsSettled)
                return FactoryResponse<bool>.CreateFail("Settled card info cannot be deleted.", ApplicationActionStatusCodes.Conflict);
            try
            {
                await _repository.DeleteAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                return FactoryResponse<bool>.CreateFail(ex.Message, ApplicationActionStatusCodes.Conflict);
            }
            return FactoryResponse<bool>.CreateSuccess(true, "Card info deleted successfully.");
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
