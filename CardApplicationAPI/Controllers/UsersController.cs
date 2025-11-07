using Microsoft.AspNetCore.Mvc;
using CardApplicationAPI.Models;
using CardApplicationAPI.Services.Interfaces;
namespace CardApplicationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;

        public UsersController(IUserService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        //[HttpGet("role/{role}")]
        //public async Task<IActionResult> GetByRole(string role)
        //{
        //    var users = await _service.GetByRoleAsync(role);
        //    return Ok(users);
        //}

        //[HttpGet("username/{username}")]
        //public async Task<IActionResult> GetByUsername(string username)
        //{
        //    var user = await _service.GetByUsersnameAsync(username);
        //    if (user == null) return NotFound();
        //    return Ok(user);
        //}

        [HttpPost]
        public async Task<IActionResult> Create(Users user)
        {
            var created = await _service.AddAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Users user)
        {
            if (id != user.Id) return BadRequest();
            await _service.UpdateAsync(user);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
