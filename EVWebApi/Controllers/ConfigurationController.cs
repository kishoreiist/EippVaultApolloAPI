using EVWebApi.DTOs.HR;
using EVWebApi.Interfaces.Services;
using EVWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.FileManager.Base;

namespace EVWebApi.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : BaseController
    {
        private readonly IConfigurationService _service;
        private readonly IAuditLogService _auditlogservice;
        public ConfigurationController(IConfigurationService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditlogservice = auditLogService;
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpPost("collection")]
        public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionDto dto)
        {
            try
            {

                if (dto == null)
                    return BadRequest("Invalid payload");

                var result = await _service.CreateCollectionAsync(dto, CurrentUserId);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Collection", "Record Created", result.Name);
                return Ok(result);

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpPut("collection/{id}")]
        public async Task<IActionResult> UpdateCollection(int id, [FromBody] CreateCollectionDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Invalid payload");

                var result = await _service.UpdateCollectionAsync(id, dto, CurrentUserId);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Collection", "Record Updated", result.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("collection")]
        public async Task<IActionResult> GetCollection([FromQuery] CollectionQueryDto dto)
        {
            try
            {
                var result = await _service.GetCollectionListAsync(dto);
                if (result == null)
                    return NotFound("Collection not found");
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("collection/{id}")]
        public async Task<IActionResult> GetCollectionById(int id)
        {
            try
            {
                var result = await _service.GetCollectionByIdAsync(id);
                if (result == null)
                    return NotFound("Collection not found");
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpDelete("collection/{id}")]
        public async Task<IActionResult> DeleteCollection(int id)
        {
            try
            {
                await _service.DeleteCollectionAsync(id);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Collection", "Record Deleted");
                return NoContent();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpPost("send_config")]
        public async Task<IActionResult> SendConfiguration([FromBody] ConfigurationRequestDto dto)
        {
            try
            {

                if (dto.CollectionId <= 0)
                    return BadRequest("CollectionId is required");

                if (dto.Emails == null || !dto.Emails.Any())
                    return BadRequest("At least one email is required");

                if (dto.ExpiryDate <= DateTime.UtcNow)
                    return BadRequest("Expiry date must be in future");

               
                var result = await _service.SendConfigurationAsync(dto, CurrentUserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to send configuration",
                    error = ex.Message
                });
            }
        }

        [HttpGet("upload/{token}")]
        public async Task<IActionResult> GetUploadDocs(string token)
        {
            try
            {
                var result = await _service.GetUploadDocsAsync(token);
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocs([FromForm] OnboardingDocsDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Token))
                    return BadRequest("Token is required");
                if (dto.Files == null || !dto.Files.Any())
                    return BadRequest("At least one file is required");
                var result = await _service.UploadDocumentsAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("requests")]
        public async Task<IActionResult> GetAllRequests([FromQuery] ConfigQueryParamsDto dto)
        {
            var result = await _service.GetAllConfigsAsync(CurrentUserId, CurrentUserType, dto);
            return Ok(result);
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("requests/{id}")]
        public async Task<IActionResult> GetConfigRequestById(int id, [FromQuery] ConfigQueryDetailDto dto)
        {
            var result = await _service.GetConfigRequestByIdAsync(id,dto);
            return Ok(result);
        }
    }
 }

