
using EVWebApi.DTOs.Cabinet;
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
    [Route("api/[controller]")]
    [ApiController]
    public class CabinetController : BaseController
    {
        private readonly ICabinetService _CabinetService;
        private readonly IAuditLogService _auditlogservice;

        public CabinetController(ICabinetService CabinetService, IAuditLogService auditLogService)
        {
            _CabinetService = CabinetService;
            _auditlogservice = auditLogService;

        }


        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CabinetQueryParameters query)
        {
            var Cabinets = await _CabinetService.GetAllAsync(query);
            string filterDetails = query.ToFilterLog();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Cabinet", "GetAll", null, filters: filterDetails);
            return Ok(Cabinets);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var Cabinet = await _CabinetService.GetByIdAsync(id);
            if (Cabinet == null) return NotFound();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Cabinet", "Get", Cabinet.CabinetName);
            return Ok(Cabinet);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCabinetDto dto)
        {
            var created = await _CabinetService.CreateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Cabinet", "Create", created.CabinetName);
            return CreatedAtAction(nameof(Get), new { id = created.CabinetId }, created);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCabinetDto dto)
        {
            if (id != dto.CabinetId)
                throw new BadRequestException("Cabinet not exists");
            var updated = await _CabinetService.UpdateAsync(dto);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Cabinet", "Update", updated.CabinetName);
            return Ok(updated);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _CabinetService.DeleteAsync(id);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Cabinet", "Delete");
            return NoContent();
        }
    }
}
