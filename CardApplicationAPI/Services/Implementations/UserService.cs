using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;
namespace CardApplicationAPI.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<IEnumerable<Users>>> GetAllAsync()
        {
            var result = await _repository.GetAllAsync();
            return FactoryResponse<IEnumerable<Users>>.CreateSuccess(result, "Users retrieved successfully.");
        }

        public async Task<InternalActionResult<Users?>> GetByIdAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
                return FactoryResponse<Users?>.CreateFail("User not found.");

            return FactoryResponse<Users?>.CreateSuccess(user, "User retrieved successfully.");
        }

        public async Task<InternalActionResult<Users>> AddAsync(Users user)
        {
            var created = await _repository.CreateAsync(user);
            return FactoryResponse<Users>.CreateSuccess(created, "User added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(Users user)
        {
            await _repository.UpdateAsync(user);
            return FactoryResponse<bool>.CreateSuccess(true, "User updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
            return FactoryResponse<bool>.CreateSuccess(true, "User deleted successfully.");
        }

        // Optional extras if you enable them later:
        // public async Task<InternalActionResult<Users?>> GetByUsernameAsync(string username)
        // {
        //     var user = await _repository.GetByUsernameAsync(username);
        //     return user != null
        //         ? FactoryResponse<Users?>.CreateSuccess(user, "User retrieved successfully.")
        //         : FactoryResponse<Users?>.CreateFailure("User not found.");
        // }

        // public async Task<InternalActionResult<IEnumerable<Users>>> GetByRoleAsync(string role)
        // {
        //     var users = await _repository.GetByRoleAsync(role);
        //     return FactoryResponse<IEnumerable<Users>>.CreateSuccess(users, "Users retrieved successfully.");
        // }
    }
}
