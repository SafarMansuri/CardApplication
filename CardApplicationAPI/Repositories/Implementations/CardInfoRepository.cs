using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace CardApplicationAPI.Repositories.Implementations
{
    public class CardInfoRepository : ICardInfoRepository
    {
        private readonly AppDbContext _context;

        public CardInfoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CardInfo>> GetAllAsync()
        {
            var entities = await _context.CardInfo.ToListAsync();
            return entities.Select(MapToModel);
        }

        public async Task<CardInfo?> GetByIdAsync(int id)
        {
            var entity = await _context.CardInfo.FirstOrDefaultAsync(u => u.Id == id);
            return entity != null ? MapToModel(entity) : null;
        }

        public async Task<CardInfo> CreateAsync(CardInfo model)
        {
            var entity = MapToEntity(model);
            entity.Createdat = DateTime.UtcNow;
            entity.Updatedat = DateTime.UtcNow;

            _context.CardInfo.Add(entity);
            await _context.SaveChangesAsync();

            return MapToModel(entity);
        }

        public async Task<CardInfo> UpdateAsync(CardInfo model)
        {
            var existing = await _context.CardInfo.FirstOrDefaultAsync(u => u.Id == model.Id);
            if (existing == null)
                throw new InvalidOperationException($"CardInfo with ID {model.Id} not found");



            existing.CardName = model.CardName;
            existing.CardNumber = model.CardNumber;
            existing.PartyName = model.PartyName;
            existing.Deduction = model.Deduction;
            existing.Date = model.Date;
            existing.AdditionalInfo = model.AdditionalInfo;
            existing.POS = model.POS;
            existing.Remarks = model.Remarks;
            existing.limitUsed = model.limitUsed;
            existing.Updatedat = model.Updatedat;

            await _context.SaveChangesAsync();
            return MapToModel(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CardInfo.FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null) return false;

            _context.CardInfo.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.CardInfo.AnyAsync(u => u.Id == id);
        }


        private static CardInfo MapToModel(CardInfoEntity entity)
        {
            return new CardInfo
            {
                Id = entity.Id,
                CardName = entity.CardName,
                CardNumber = entity.CardNumber,
                PartyName = entity.PartyName,
                Deduction = entity.Deduction,
                Date = entity.Date,
                AdditionalInfo = entity.AdditionalInfo,
                POS = entity.POS,
                Remarks = entity.Remarks,
                limitUsed = entity.limitUsed,
                Createdat = entity.Createdat,
                Updatedat = entity.Updatedat
            };
        }

        private static CardInfoEntity MapToEntity(CardInfo model)
        {
            return new CardInfoEntity
            {
                Id = model.Id,
                CardName = model.CardName,
                CardNumber = model.CardNumber,
                PartyName = model.PartyName,
                Deduction = model.Deduction,
                Date = model.Date,
                AdditionalInfo = model.AdditionalInfo,
                POS = model.POS,
                Remarks = model.Remarks,
                limitUsed = model.limitUsed,
                Createdat = model.Createdat,
                Updatedat = model.Updatedat
            };

        }
    }
}
