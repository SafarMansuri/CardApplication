using CardApplicationAPI.Models;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IUserRepository
    {
        // 🔹 Basic CRUD
        Task<IEnumerable<Users>> GetAllAsync();
        Task<Users?> GetByIdAsync(int id);
        Task<Users> CreateAsync(Users model);
        Task<Users> UpdateAsync(Users model);
        Task<bool> DeleteAsync(int id);
        // 🔹 Utility Methods
        Task<bool> ExistsAsync(int id);
    //    Task<bool> IsUsernameTakenAsync(string username);
      //  Task<Users?> GetByUsernameAsync(string username);
    }
}
