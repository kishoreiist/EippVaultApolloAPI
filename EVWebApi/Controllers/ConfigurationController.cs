using EVWebApi.DTOs.HR;
using EVWebApi.Helpers;
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
        [HttpGet("collection_list")]
        public async Task<IActionResult> GetCollectionDropDown([FromQuery] CollectionDropDownQueryDto dto)
        {
            try
            {
                var result = await _service.GetCollectionDropDownListAsync(dto);

                return Ok(result);
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
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Configuration", "Record Created");
                return Ok(result);
            }
            catch (Exception ex)
            {
                //return StatusCode(500, new
                //{
                //    message = "Failed to send configuration",
                //    error = ex.InnerException.Message
                //});
                throw;
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
                var result = await _service.MainUploadDocumentsAsync(dto);
                //await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Config_Upload", "Record Created", result.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("requests/list")]
        public async Task<IActionResult> GetAllRequests([FromQuery] ConfigQueryParamsDto dto)
        {
            var result = await _service.GetAllConfigsAsync(CurrentUserId, "super_admin", dto);
            return Ok(result);
        }

        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("requests")]
        public async Task<IActionResult> GetConfigRequest([FromQuery] ConfigQueryDetailDto dto)
        {
            var result = await _service.GetConfigRequestsAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var result = await _service.GetOnboardingDocumentStream(id);
            if (result == null) return NotFound();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Hr Docs", "Document View");

            var contentType = FileContentTypeDetectHelper.GetContentType(result.FilePath);

            return File(result.Stream, contentType, enableRangeProcessing: true);

        }

        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var download = await _service.GetOnboardingDocumentStream(id);
            if (download == null) return NotFound();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Hr Docs", "Document Download");

            var contentType = FileContentTypeDetectHelper.GetContentType(download.FileName);

            return File(download.Stream, contentType, download.FileName, enableRangeProcessing: true);
        }

        [Authorize(Roles = "admin,super_admin")]
        [HttpPost("onboarding_excel_upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] OnboardingUploadDto dto)
        {
            try
            {

                var result = await _service.OnboardingExcelUploadAsync(dto, CurrentUserId);

                return Ok(result);

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("onboarding_failed_report/{batchId}")]
        public async Task<IActionResult> ExportFailedRows(int batchId)
        {
            var (bytes, fileName) = await _service.ExportFailedRowsAsync(batchId);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);

        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpPost("batch/{batchId}/confirm")]
        public async Task<IActionResult> ConfirmOnboardingBatch(int batchId)
        {


            var result = await _service.ConfirmOnboardingBatchAsync(batchId, CurrentUserId);

            return Ok(result);
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("export_onboarding_report")]
        public async Task<IActionResult> ExportOnboardingReport([FromQuery] ExportOnboardingReportQuery query)
        {
            var result = await _service.ExportOnboardingReport(query);

            return File(
                result.Item1,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Item2);
        }
        [Authorize(Roles = "admin,super_admin")]
        [HttpGet("status_count")]
        public async Task<IActionResult> GetCandidatesStatusCount([FromQuery] StatusCountQueryParamDto dto)
        {
            var result = await _service.GetCandidatesStatusCountAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "admin,super_admin")]
        [HttpPost("request_laptop")]
        public async Task<IActionResult> RequestLaptop(RequestLaptopDto dto)
        {
            try
            {

                var result = await _service.SendLaptopRequestMailAsync(dto);
                if (result)
                { 
                    await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Configuration", "LaptopRequest Send", dto.To);
                    return Ok(new
                    {
                        message = "Laptop request sent successfully"
                    });
                }
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Configuration", "LaptopRequest Failed", dto.To);
                return StatusCode(500, new
                {
                    message = "Failed to send laptop request"
                });

            }
            catch (Exception ex) 
            {
                throw;
            }

        }
    }
 }

