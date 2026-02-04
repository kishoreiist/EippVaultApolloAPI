using EVWebApi.DTOs.Group;
using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
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
            try
            {
                await _groupService.DeleteAsync(id);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Group", "Record Deleted");
                return NoContent();
            }
            catch (ConflictException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "group delete failed",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("dropdown")]
        public async Task<IActionResult> GetGroupsForDropdown()
        {
            var groups = await _groupService.GetGroupsForDropdownAsync();

            return Ok(groups);
        }

        //----------------email grp----------------

        [HttpPost("email_group")]
        public async Task<IActionResult> CreateEmailGroup([FromBody] CreateEmailGroupDto dto)
        {
            var created = await _groupService.CreateEmailGroupAsync(dto);

            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Email Group", "Record Created", created.GroupName);

            return Ok(created);
        }

        [HttpGet("email_group")]
        public async Task<IActionResult> GetAllEmailGroupForDropDown()
        {
            var groups = await _groupService.GetallEmailGroupForDropDownAsync();
            return Ok(groups);
        }

        [HttpPut("email_group/{id}")]
        public async Task<IActionResult> UpdateEmailGroup([FromBody] EmailGroupDto dto)
        {
            var updated = await _groupService.UpdateEmailGroupAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Email Group", "Record Updated", updated.GroupName);
            return Ok(updated);

        }

        [HttpDelete("email_group/{id}")]
        public async Task<IActionResult> DeleteEmailGroup(int id)
        {
            try
            {
                await _groupService.DeleteEmailGroupAsync(id);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Email Group", "Record Deleted");
                return NoContent();
            }
            catch (ConflictException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Email group delete failed",
                    Error = ex.Message
                });
            }
        }
    }
}
