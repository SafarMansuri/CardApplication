using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardApplicationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Master")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<InternalActionResult<DashboardSummary>>> GetSummary(CancellationToken cancellationToken)
        {
            var result = await _service.GetSummaryAsync(cancellationToken);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("monthly-flow")]
        public async Task<ActionResult<InternalActionResult<IReadOnlyList<MonthlyFlowPoint>>>> GetMonthlyFlow(
            [FromQuery] int months = 12,
            CancellationToken cancellationToken = default)
        {
            var result = await _service.GetMonthlyFlowAsync(months, cancellationToken);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("swipes-daily")]
        public async Task<ActionResult<InternalActionResult<IReadOnlyList<DailySwipePoint>>>> GetSwipesDaily(
            [FromQuery] int days = 30,
            CancellationToken cancellationToken = default)
        {
            var result = await _service.GetSwipesDailyAsync(days, cancellationToken);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("top-parties")]
        public async Task<ActionResult<InternalActionResult<IReadOnlyList<TopPartyOwedItem>>>> GetTopParties(
            [FromQuery] int limit = 5,
            CancellationToken cancellationToken = default)
        {
            var result = await _service.GetTopPartiesAsync(limit, cancellationToken);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}