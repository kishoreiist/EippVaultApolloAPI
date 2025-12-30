using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.User;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services;
using EVWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    public class GroupController : BaseController
    {
        private readonly IGroupService _groupService;
        private readonly IAuditLogService _auditlogservice;


        public GroupController(IGroupService groupService, IAuditLogService auditlogservice)
        {
            _groupService = groupService;
            _auditlogservice = auditlogservice;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GroupQueryParameters query)
        {
            var groups = await _groupService.GetAllAsync(query);
            string filterDetails = query.ToFilterLog();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Group", "All Records Retrieved", null, filters: filterDetails);
            return Ok(groups);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var group = await _groupService.GetByIdAsync(id);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Group", "Record Retrieved", group.GroupName);
            if (group == null) return NotFound();
            return Ok(group);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGroupDto dto)
        {
            var created = await _groupService.CreateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Group", "Record Created", created.GroupName);
            return CreatedAtAction(nameof(Get), new { id = created.GroupId }, created);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateGroupDto dto)
        {
            if (id != dto.GroupId) return BadRequest();
            var updated = await _groupService.UpdateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Group", "Record Updated", updated.GroupName);
            return Ok(new { data = updated });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _groupService.DeleteAsync(id);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Group", "Record Deleted");
            return NoContent();
        }

    }
}
