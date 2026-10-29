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

        private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "id", "date", "partyName", "cardName", "cardNumber", "bankName", "deduction",
            "profit", "pos", "model", "swipePerson", "merchant", "limitUsed",
            "isSettled", "settledAt", "createdat", "updatedat"
        };

        public async Task<InternalActionResult<PaginatedResult<CardInfo>>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            bool? isSettled = null,
            bool? limitUsed = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var sortValidation = ValidateSort(ref sortBy, ref sortDir);
            if (sortValidation != null)
                return FactoryResponse<PaginatedResult<CardInfo>>.CreateFail(sortValidation, ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.GetAllAsync(pagination, search, isSettled, limitUsed, sortBy, sortDir);
            return FactoryResponse<PaginatedResult<CardInfo>>.CreateSuccess(result, "Card info retrieved successfully.");
        }

        private static string? ValidateSort(ref string? sortBy, ref string? sortDir)
        {
            sortBy = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.Trim();
            if (sortBy != null && !AllowedSortFields.Contains(sortBy))
                return "Invalid sortBy field.";

            sortDir = string.IsNullOrWhiteSpace(sortDir) ? null : sortDir.Trim().ToLowerInvariant();
            if (sortDir != null && sortDir != "asc" && sortDir != "desc")
                return "SortDir must be asc or desc.";

            return null;
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
            if (string.IsNullOrWhiteSpace(model.BankName) || model.BankName.Length > 50)
                return "BankName is required and cannot exceed 50 characters.";
            if (model.Deduction < 0 || model.Profit < 0)
                return "Deduction and Profit cannot be negative.";
            return null;
        }
    }
}
