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
    public class CardInfoController : ControllerBase
    {
        private readonly ICardInfoService _service;

        public CardInfoController(ICardInfoService service)
        {
            _service = service;
        }

        // ✅ GET: api/CardInfo
        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<CardInfo>>>> GetAll([FromQuery] PaginationParams pagination)
        {
            var result = await _service.GetAllAsync(pagination);
            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ GET: api/CardInfo/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<CardInfo?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ POST: api/CardInfo
        [HttpPost]
        public async Task<ActionResult<InternalActionResult<CardInfo>>> Create([FromBody] CardInfo user)
        {
            var result = await _service.AddAsync(user);
            if (result.StatusCode != ApplicationActionStatusCodes.Success)
                return StatusCode((int)result.StatusCode, result);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        // ✅ PUT: api/CardInfo/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] CardInfo user)
        {
            if (id != user.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("Card info ID mismatch.", ApplicationActionStatusCodes.BadRequest));

            var result = await _service.UpdateAsync(user);
            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ DELETE: api/CardInfo/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
