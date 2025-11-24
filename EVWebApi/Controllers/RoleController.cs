using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.Role;
using EVWebApi.Filters;
using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequiresPermission("manage_users")]
    public class RoleController : BaseController
    {
        private readonly IRoleService _roleService;
        private readonly IAuditLogService _auditlogservice;


        public RoleController(IRoleService roleService, IAuditLogService auditlogservice)
        {
            _roleService = roleService;
            _auditlogservice = auditlogservice;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] RoleQueryParameters query)
        {
            var roles = await _roleService.GetAllAsync(query);
            await _auditlogservice.LogAsync(CurrentUserId, "Role", "GetAll");
            return Ok(roles);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var role = await _roleService.GetByIdAsync(id);
            await _auditlogservice.LogAsync(CurrentUserId, "Role", "Get", id);
            if (role == null) return NotFound();
            return Ok(role);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
        {
            var created = await _roleService.CreateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, "Role", "Create", created.RoleId);
            return CreatedAtAction(nameof(Get), new { id = created.RoleId }, created);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleDto dto)
        {
            if (id != dto.RoleId) return BadRequest();
            var updated = await _roleService.UpdateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, "Role", "Update", id);
            return Ok(updated);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _roleService.DeleteAsync(id);
            await _auditlogservice.LogAsync(CurrentUserId, "Role", "Delete", id);
            return NoContent();
        }
    }
}
