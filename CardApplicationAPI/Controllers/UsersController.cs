using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CardApplicationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Master")]
    public class UsersController : ControllerBase
    {
        private static readonly string[] AllowedRoles = { "Master", "Admin", "User" };

        private readonly IUserService _service;

        public UsersController(IUserService service)
        {
            _service = service;
        }

        // ✅ GET: api/users
        [HttpGet]
        public async Task<ActionResult<InternalActionResult<PaginatedResult<Users>>>> GetAll(
            [FromQuery] PaginationParams pagination,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDir = null)
        {
            var result = await _service.GetAllAsync(pagination, search, role, sortBy, sortDir);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<InternalActionResult<UserRoleSummary>>> GetSummary()
        {
            var result = await _service.GetSummaryAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<Users?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ POST: api/users
        [HttpPost]
        public async Task<ActionResult<InternalActionResult<Users>>> Create([FromBody] Users user)
        {
            var result = await _service.AddAsync(user);
            if (result.StatusCode != ApplicationActionStatusCodes.Success)
                return StatusCode((int)result.StatusCode, result);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        // ✅ PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] Users user)
        {
            if (id != user.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("User ID mismatch.", ApplicationActionStatusCodes.BadRequest));

            var result = await _service.UpdateAsync(user);
            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
