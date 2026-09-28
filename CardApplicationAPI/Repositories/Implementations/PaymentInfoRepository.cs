using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class PaymentInfoRepository : IPaymentInfoRepository
    {
        private readonly AppDbContext _context;

        public PaymentInfoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<PaymentInfo>> GetAllAsync(PaginationParams pagination, string? partyName)
        {
            var query = _context.PaymentInfo.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(partyName))
            {
                var normalizedPartyName = partyName.Trim();
                query = query.Where(x => x.PartyName == normalizedPartyName);
            }

            var result = await query.OrderBy(x => x.PartyName).ToPaginatedResultAsync(pagination);
            return new PaginatedResult<PaymentInfo>
            {
                Items = result.Items.Select(x => new PaymentInfo
                {
                    Id = x.Id,
                    Date = x.Date,
                    PartyName = x.PartyName,
                    SwipeCount = x.SwipeCount,
                    SwipeAmount = x.SwipeAmount,
                    TotalAmount = x.TotalAmount,
                    Profit = x.Profit,
                    TotalProfit = x.TotalProfit,
                    LimitUsed = x.LimitUsed,
                    CardName = x.CardName,
                    Merchant = x.Merchant,
                    SwipePerson = x.SwipePerson,
                    Remarks = x.Remarks,
                    Createdat = x.Createdat,
                    Updatedat = x.Updatedat
                }),
                Pagination = result.Pagination
            };
        }
    }
}
