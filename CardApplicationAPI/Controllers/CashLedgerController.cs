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
    public class CashLedgerController : ControllerBase
    {
        private readonly ICashLedgerService _service;

        public CashLedgerController(ICashLedgerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<CashLedger>>>> GetAll(
            [FromQuery] PaginationParams pagination,
            [FromQuery] string? search = null,
            [FromQuery] string? transactionType = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDir = null)
        {
            var result = await _service.GetAllAsync(pagination, search, transactionType, sortBy, sortDir);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<InternalActionResult<CashLedger?>>> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("type/{transactionType}")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<CashLedger>>>> GetByType(string transactionType, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetByTypeAsync(transactionType, pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary/opening")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetOpeningBalance()
        {
            var result = await _service.GetOpeningBalanceAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary/in")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetCashIn()
        {
            var result = await _service.GetCashInAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary/out")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetCashOut()
        {
            var result = await _service.GetCashOutAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary/current")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetCurrentBalance()
        {
            var result = await _service.GetCurrentBalanceAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost]
        public async Task<ActionResult<InternalActionResult<CashLedger>>> Create([FromBody] CashLedger model)
        {
            var result = await _service.AddAsync(model);
            if (result.StatusCode != ApplicationActionStatusCodes.Success)
                return StatusCode((int)result.StatusCode, result);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(long id, [FromBody] CashLedger model)
        {
            if (id != model.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("Cash ledger ID mismatch.", ApplicationActionStatusCodes.BadRequest));

            var result = await _service.UpdateAsync(model);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{id:long}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
