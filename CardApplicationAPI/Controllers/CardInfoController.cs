using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CardApplicationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardInfoController : ControllerBase
    {
        private readonly ICardInfoService _service;

        public CardInfoController(ICardInfoService service)
        {
            _service = service;
        }

        // ✅ GET: api/CardInfo
        [HttpGet]
        public async Task<ActionResult<InternalActionResult<IEnumerable<CardInfo>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return result; // InternalActionResult already includes status/message
        }

        // ✅ GET: api/CardInfo/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<CardInfo?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return result;
        }

        // ✅ POST: api/CardInfo
        [HttpPost]
        public async Task<ActionResult<InternalActionResult<CardInfo>>> Create([FromBody] CardInfo user)
        {
            var result = await _service.AddAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        // ✅ PUT: api/CardInfo/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] CardInfo user)
        {
            if (id != user.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("User ID mismatch."));

            var result = await _service.UpdateAsync(user);
            return result;
        }

        // ✅ DELETE: api/CardInfo/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result;
        }
    }
}
