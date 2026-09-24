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
        public async Task<ActionResult<InternalActionResult<IEnumerable<StockItem>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return result;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<StockItem?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result;
        }

        [HttpGet("by-party/{partyName}")]
        public async Task<ActionResult<InternalActionResult<IEnumerable<StockItem>>>> GetByPartyName(string partyName)
        {
            var result = await _service.GetByPartyNameAsync(partyName);
            return result;
        }

        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<InternalActionResult<IEnumerable<StockItem>>>> GetByStatus(string status)
        {
            var result = await _service.GetByStatusAsync(status);
            return result;
        }

        [HttpGet("by-inout/{inOut}")]
        public async Task<ActionResult<InternalActionResult<IEnumerable<StockItem>>>> GetByInOut(string inOut)
        {
            var result = await _service.GetByInOutAsync(inOut);
            return result;
        }

        [HttpPost]
        public async Task<ActionResult<InternalActionResult<StockItem>>> Create([FromBody] StockItem model)
        {
            var result = await _service.AddAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] StockItem model)
        {
            if (id != model.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("Stock item ID mismatch."));

            var result = await _service.UpdateAsync(model);
            return result;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result;
        }
    }
}
