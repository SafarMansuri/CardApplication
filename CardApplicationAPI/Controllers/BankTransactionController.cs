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
    public class BankTransactionController : ControllerBase
    {
        private readonly IBankTransactionService _service;

        public BankTransactionController(IBankTransactionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<BankTransaction>>>> GetAll([FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetAllAsync(pagination);
            return result;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InternalActionResult<BankTransaction?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result;
        }

        [HttpGet("account/{accountId:int}")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<BankTransaction>>>> GetByAccountId(int accountId, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetByAccountIdAsync(accountId, pagination);
            return result;
        }

        [HttpPost]
        public async Task<ActionResult<InternalActionResult<BankTransaction>>> Create([FromBody] BankTransaction model)
        {
            var result = await _service.AddAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        [HttpPost("transfer")]
        public async Task<ActionResult<InternalActionResult<BankTransferResult>>> Transfer([FromBody] BankTransferRequest request)
        {
            var result = await _service.TransferAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] BankTransaction model)
        {
            if (id != model.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("Bank transaction ID mismatch."));

            var result = await _service.UpdateAsync(model);
            return result;
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result;
        }
    }
}
