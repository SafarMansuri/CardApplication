using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IUserRepository
    {
        // 🔹 Basic CRUD
        Task<PaginatedResult<Users>> GetAllAsync(PaginationParams pagination);
        Task<Users?> GetByIdAsync(int id);
        Task<Users> CreateAsync(Users model);
        Task<Users> UpdateAsync(Users model);
        Task<bool> DeleteAsync(int id);
        // 🔹 Utility Methods
        Task<bool> ExistsAsync(int id);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<Users?> GetByUsernameAsync(string username);
    }
}
