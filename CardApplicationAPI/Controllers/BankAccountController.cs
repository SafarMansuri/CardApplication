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
    public class BankAccountController : ControllerBase
    {
        private readonly IBankAccountService _service;

        public BankAccountController(IBankAccountService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<BankAccount>>>> GetAll([FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetAllAsync(pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InternalActionResult<BankAccount?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary/total")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetTotalBalance()
        {
            var result = await _service.GetTotalBalanceAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary/accounts")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<BankAccountBalanceSummary>>>> GetBalanceSummary([FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetBalanceSummaryAsync(pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost]
        public async Task<ActionResult<InternalActionResult<BankAccount>>> Create([FromBody] BankAccount model)
        {
            var result = await _service.AddAsync(model);
            if (result.StatusCode != ApplicationActionStatusCodes.Success)
                return StatusCode((int)result.StatusCode, result);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] BankAccount model)
        {
            if (id != model.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("Bank account ID mismatch.", ApplicationActionStatusCodes.BadRequest));

            var result = await _service.UpdateAsync(model);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
