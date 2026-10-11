using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            [FromQuery] PaginationParams pagination, [FromQuery] string? partyName, [FromQuery] string? inOut,
            [FromQuery] string? paymentType, [FromQuery] string? status)
        {
            var result = await _service.GetAllAsync(pagination, partyName, inOut, paymentType, status);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary/parties")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockPartyPaymentSummary>>>> GetPartySummaries(
            [FromQuery] PaginationParams pagination, [FromQuery] string? partyName, [FromQuery] string? paymentType, [FromQuery] string? status)
        {
            var result = await _service.GetPartySummariesAsync(pagination, partyName, paymentType, status);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary/totals")]
        public async Task<ActionResult<InternalActionResult<StockPaymentTotals>>> GetTotals()
        {
            var result = await _service.GetTotalsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<StockPaymentInfo?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("stock/{stockItemId}")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockPaymentInfo>>>> GetByStockItemId(int stockItemId, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetByStockItemIdAsync(stockItemId, pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id}/history")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockPaymentHistory>>>> GetHistory(int id, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetHistoryAsync(id, pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPatch("{id}/settle")]
        [Authorize(Roles = "Admin,Master")]
        public async Task<ActionResult<InternalActionResult<StockPaymentInfo>>> Settle(int id, [FromBody] StockPaymentSettlementRequest request)
        {
            if (!TryGetUser(out var userId, out var username)) return Unauthorized();
            var result = await _service.SettleAsync(id, request, userId, username);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPatch("{id}/reset")]
        [Authorize(Roles = "Admin,Master")]
        public async Task<ActionResult<InternalActionResult<StockPaymentInfo>>> Reset(int id, [FromBody] StockPaymentResetRequest request)
        {
            if (!TryGetUser(out var userId, out var username)) return Unauthorized();
            var result = await _service.ResetAsync(id, request, userId, username);
            return StatusCode((int)result.StatusCode, result);
        }

        private bool TryGetUser(out int userId, out string username)
        {
            username = User.Identity?.Name ?? string.Empty;
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && !string.IsNullOrWhiteSpace(username);
        }
    }
}
