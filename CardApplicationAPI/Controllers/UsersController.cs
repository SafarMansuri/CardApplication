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
        public async Task<ActionResult<InternalActionResult<IEnumerable<Users>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return result; // InternalActionResult already includes status/message
        }

        // ✅ GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InternalActionResult<Users?>>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            return result;
        }

        // ✅ POST: api/users
        [HttpPost]
        public async Task<ActionResult<InternalActionResult<Users>>> Create([FromBody] Users user)
        {
            if (!AllowedRoles.Contains(user.Role))
                return BadRequest(FactoryResponse<Users>.CreateFail("Invalid role. Allowed roles: Master, Admin, User."));

            var result = await _service.AddAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = result.ResultObject?.Id }, result);
        }

        // ✅ PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Update(int id, [FromBody] Users user)
        {
            if (id != user.Id)
                return BadRequest(FactoryResponse<bool>.CreateFail("User ID mismatch."));

            if (!AllowedRoles.Contains(user.Role))
                return BadRequest(FactoryResponse<bool>.CreateFail("Invalid role. Allowed roles: Master, Admin, User."));

            var result = await _service.UpdateAsync(user);
            return result;
        }

        // ✅ DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<InternalActionResult<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result;
        }
    }
}
