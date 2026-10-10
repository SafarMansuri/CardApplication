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
    }
}