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
    public class StockPaymentInfoController : ControllerBase
    {
        private readonly IStockPaymentInfoService _service;

        public StockPaymentInfoController(IStockPaymentInfoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockPaymentInfo>>>> GetAll([FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetAllAsync(pagination);
            return result;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<StockPaymentInfo?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result;
        }

        [HttpGet("stock/{stockItemId}")]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<StockPaymentInfo>>>> GetByStockItemId(int stockItemId, [FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetByStockItemIdAsync(stockItemId, pagination);
            return result;
        }

        [HttpPost]
        public async Task<ActionResult<InternalActionResult<StockPaymentInfo>>> Create([FromBody] StockPaymentInfo model)
        {
            var result = await _service.AddAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] StockPaymentInfo model)
        {
            if (id != model.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("Stock payment ID mismatch."));

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
