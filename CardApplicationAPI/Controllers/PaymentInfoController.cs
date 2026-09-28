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
            [FromQuery] string? partyName)
        {
            var result = await _service.GetAllAsync(pagination, partyName);
            return result;
        }
    }
}