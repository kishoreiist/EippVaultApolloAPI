using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EVWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

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
            try
            {
                var users = await _userService.GetAllAsync(query);
                string filterDetails = query.ToFilterLog();
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "User", "All Records Retrieved", null, filters: filterDetails);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "User Fetch failed",
                    Error = ex.Message
                });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "User", "Record Retrieved", user.Username);
            return Ok(user);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            try
            {
                var created = await _userService.CreateAsync(dto);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "User", "User created", created.Username);
                return CreatedAtAction(nameof(Get), new { id = created.UserId }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "User creation failed",
                    Error = ex.Message
                });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            if (id != dto.UserId)
                throw new BadRequestException("User not exists");
            var updated = await _userService.UpdateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "User", "Record Updated", updated.Username);
            return Ok(updated);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _userService.DeleteAsync(id);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "User", "Record Deleted");
                return Ok("User Deleted succesfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "User deletion failed",
                    Error = ex.Message
                });
            }
        }

        //--------------------email grp--------------

        [HttpGet("email_group/{id}")]
        public async Task<IActionResult> GetUsersByEmailGroup(int id)
        {
            var user = await _userService.GetUserByEmailGroupAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }
    }
}
