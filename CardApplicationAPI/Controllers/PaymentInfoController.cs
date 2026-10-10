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
    public class PaymentInfoController : ControllerBase
    {
        private readonly IPaymentInfoService _service;

        public PaymentInfoController(IPaymentInfoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<PaymentInfo>>>> GetAll(
            [FromQuery] PaginationParams pagination,
            [FromQuery] string? partyName,
            [FromQuery] string? status)
        {
            var result = await _service.GetAllAsync(pagination, partyName, status);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPatch("{id}/mark-paid")]
        [Authorize(Roles = "Admin,Master")]
        public async Task<ActionResult<InternalActionResult<PaymentInfo>>> MarkPaid(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized(FactoryResponse<PaymentInfo>.CreateFail("Authenticated user is invalid.", ApplicationActionStatusCodes.Unauthorized));

            var result = await _service.MarkPaidAsync(id, userId, User.Identity?.Name ?? string.Empty);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
