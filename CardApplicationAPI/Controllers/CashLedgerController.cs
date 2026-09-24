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
        public async Task<ActionResult<InternalActionResult<IEnumerable<CashLedger>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return result;
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<InternalActionResult<CashLedger?>>> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result;
        }

        [HttpGet("type/{transactionType}")]
        public async Task<ActionResult<InternalActionResult<IEnumerable<CashLedger>>>> GetByType(string transactionType)
        {
            var result = await _service.GetByTypeAsync(transactionType);
            return result;
        }

        [HttpGet("summary/opening")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetOpeningBalance()
        {
            var result = await _service.GetOpeningBalanceAsync();
            return result;
        }

        [HttpGet("summary/in")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetCashIn()
        {
            var result = await _service.GetCashInAsync();
            return result;
        }

        [HttpGet("summary/out")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetCashOut()
        {
            var result = await _service.GetCashOutAsync();
            return result;
        }

        [HttpGet("summary/current")]
        public async Task<ActionResult<InternalActionResult<decimal>>> GetCurrentBalance()
        {
            var result = await _service.GetCurrentBalanceAsync();
            return result;
        }

        [HttpPost]
        public async Task<ActionResult<InternalActionResult<CashLedger>>> Create([FromBody] CashLedger model)
        {
            var result = await _service.AddAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(long id, [FromBody] CashLedger model)
        {
            if (id != model.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("Cash ledger ID mismatch."));

            var result = await _service.UpdateAsync(model);
            return result;
        }

        [HttpDelete("{id:long}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result;
        }
    }
}
