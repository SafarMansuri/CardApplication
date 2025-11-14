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

        public async Task<InternalActionResult<IEnumerable<CardInfo>>> GetAllAsync()
        {
            var result = await _repository.GetAllAsync();
            return FactoryResponse<IEnumerable<CardInfo>>.CreateSuccess(result, "Users retrieved successfully.");
        }

        public async Task<InternalActionResult<CardInfo?>> GetByIdAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
                return FactoryResponse<CardInfo?>.CreateFail("User not found.");

            return FactoryResponse<CardInfo?>.CreateSuccess(user, "User retrieved successfully.");
        }

        public async Task<InternalActionResult<CardInfo>> AddAsync(CardInfo user)
        {
            var created = await _repository.CreateAsync(user);
            return FactoryResponse<CardInfo>.CreateSuccess(created, "User added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(CardInfo user)
        {
            await _repository.UpdateAsync(user);
            return FactoryResponse<bool>.CreateSuccess(true, "User updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
            return FactoryResponse<bool>.CreateSuccess(true, "User deleted successfully.");
        }
    }
}
