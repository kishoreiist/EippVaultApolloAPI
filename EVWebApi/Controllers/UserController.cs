using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Filters;
using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EVWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [RequiresPermission("manage_users")]

    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditlogservice;

        public UserController(IUserService userService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _auditlogservice = auditLogService;
           
        }


        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] UserQueryParameters query)
        {
            var users = await _userService.GetAllAsync(query);
            await _auditlogservice.LogAsync(CurrentUserId, "User","GetAll");
            return Ok(users);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();
            await _auditlogservice.LogAsync(CurrentUserId, "User", "Get", id);
            return Ok(user);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var created = await _userService.CreateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, "User", "Create", created.UserId);
            return CreatedAtAction(nameof(Get), new { id = created.UserId }, created);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            if (id != dto.UserId)
                throw new BadRequestException("User not exists");
            var updated = await _userService.UpdateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, "User", "Update", id);
            return Ok(updated);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteAsync(id);
            await _auditlogservice.LogAsync(CurrentUserId, "User", "Delete", id);
            return NoContent();
        }
    }
}
