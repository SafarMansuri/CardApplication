using CardApplicationAPI.Models;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<Users>> GetAllAsync();
        Task<Users?> GetByIdAsync(int id);
     //   Task<Users?> GetByUsersnameAsync(string Usersname);
        Task<Users> AddAsync(Users Users);
        Task UpdateAsync(Users Users);
        Task DeleteAsync(int id);
       // Task<IEnumerable<Users>> GetByRoleAsync(string role);
    }
}
