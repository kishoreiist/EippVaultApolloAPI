using EVWebApi.DTOs.HR;
using EVWebApi.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentAccessReqController : BaseController
    {
        private readonly IDocAccessReqService _accessService;
        private readonly IAuditLogService _auditlogservice;

        public DocumentAccessReqController(IDocAccessReqService accessService, IAuditLogService auditLogService)
        {
            _accessService = accessService;
            _auditlogservice = auditLogService;

        }


        [HttpPost]
        public async Task<IActionResult> RequestAccess([FromBody] AccessRequestDto dto)
        {
            try
            {
                

                await _accessService.RequestAccessAsync(CurrentUserId, dto);

                return Ok(new { message = "Request sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("access_action")]
        public async Task<IActionResult> HandleAccessAction([FromBody] AccessActionDto dto)
        {
            try
            {
               

                await _accessService.HandleAccessRequestAsync(CurrentUserId, dto);

                return Ok(new { message = "Action completed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


    }
}
