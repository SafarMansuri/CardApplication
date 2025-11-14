using CardApplicationAPI.Models;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface ICardInfoRepository
    {
        Task<IEnumerable<CardInfo>> GetAllAsync();
        Task<CardInfo?> GetByIdAsync(int id);
        Task<CardInfo> CreateAsync(CardInfo model);
        Task<CardInfo> UpdateAsync(CardInfo model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        
    }
}
