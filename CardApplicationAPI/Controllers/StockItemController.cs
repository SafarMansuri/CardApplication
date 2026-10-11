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
    public class StockItemController : ControllerBase
    {
        private readonly IStockItemService _service;

        public StockItemController(IStockItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockItem>>>> GetAll([FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetAllAsync(pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<StockItem?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("by-party/{partyName}")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockItem>>>> GetByPartyName(string partyName, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetByPartyNameAsync(partyName, pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockItem>>>> GetByStatus(string status, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetByStatusAsync(status, pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("by-inout/{inOut}")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockItem>>>> GetByInOut(string inOut, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetByInOutAsync(inOut, pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost]
        public async Task<ActionResult<InternalActionResult<StockItem>>> Create([FromBody] StockItem model)
        {
            var result = await _service.AddAsync(model);
            if (result.StatusCode != ApplicationActionStatusCodes.Success)
                return StatusCode((int)result.StatusCode, result);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] StockItem model)
        {
            if (id != model.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("Stock item ID mismatch."));

            var result = await _service.UpdateAsync(model);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
