using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardApplicationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockPaymentInfoController : ControllerBase
    {
        private readonly IStockPaymentInfoService _service;

        public StockPaymentInfoController(IStockPaymentInfoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockPaymentInfo>>>> GetAll(
            [FromQuery] PaginationParams pagination,
            [FromQuery] string? partyName,
            [FromQuery] string? inOut)
        {
            var result = await _service.GetAllAsync(pagination, partyName, inOut);
            return result;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<StockPaymentInfo?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result;
        }

        [HttpGet("stock/{stockItemId}")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockPaymentInfo>>>> GetByStockItemId(int stockItemId, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetByStockItemIdAsync(stockItemId, pagination);
            return result;
        }
    }
}
