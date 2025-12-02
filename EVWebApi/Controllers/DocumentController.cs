using EVWebApi.DTOs.Document;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : BaseController
    {
        private readonly IDocumentService _documentService;
        private readonly IAuditLogService _auditlogservice;

        public DocumentController(IDocumentService documentService, IAuditLogService auditLogService)
        {
            _documentService = documentService;
            _auditlogservice = auditLogService;
        }

        //Upload Document
       [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto dto)
        {
            var result = await _documentService.UploadDocument(dto, CurrentUserId);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername,"Document", "Document_Upload", result.FileName,result.CabinetId);// need to pass index fileds as filters

            return Ok(result);
        }

        // Get document metadata
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            var doc = await _documentService.GetDocument(id);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Get", doc.FileName);
            return Ok(doc);
        }

        // Get all documents by CabinetId
        [HttpGet("cabinet/{cabinetId}")]
        public async Task<IActionResult> GetDocumentsByCabinetId(int cabinetId, [FromQuery] DocumentQueryParameters query)
        {
            var docs = await _documentService.GetDocumentsByCabinetId(cabinetId, query);
            string filterDetails = query.ToFilterLog();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document_Get", null,cabinetId, null, filters: filterDetails);

            return Ok(docs);
        }

        // PDF Preview (stream)
        //[HttpGet("{id}/preview")]
        //public async Task<IActionResult> PreviewDocument(int id)
        //{
        //    var fileStream = await _documentService.GetDocumentStream(id);
        //    if (fileStream == null) return NotFound();
        //    await _auditlogservice.LogAsync(CurrentUserId, "Document", "Get", id);
        //    return File(fileStream, "application/pdf");
        //}

        // Download
        //[HttpGet("{id}/download")]
        //public async Task<IActionResult> DownloadDocument(int id)
        //{
        //    var download = await _documentService.GetDocumentForDownload(id);
        //    if (download == null) return NotFound();
        //    await _auditlogservice.LogAsync(CurrentUserId, "Document", "Get", id);
        //    return File(download.Stream, "application/octet-stream", download.FileName);
        //}

        //// Archive
        //[HttpPut("{id}/archive")]
        //public async Task<IActionResult> ArchiveDocument(int id)
        //{
        //    await _documentService.ArchiveDocument(id);
        //    await _auditlogservice.LogAsync(CurrentUserId, "Document", "Update", id);
        //    return Ok(new { message = "Document archived" });
        //}

        //// Restore
        //[HttpPut("{id}/restore")]
        //public async Task<IActionResult> RestoreDocument(int id)
        //{
        //    await _documentService.RestoreDocument(id);
        //    await _auditlogservice.LogAsync(CurrentUserId, "Document", "Update", id);
        //    return Ok(new { message = "Document restored" });
        //}

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] UpdateDocumentDto dto)
        {
            var updated = await _documentService.UpdateDocumentAsync(id, dto);

            if (updated == null)
                return NotFound(new { message = "Document not found" });

            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document_Update",updated.FileName,updated.CabinetId);

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            bool success = await _documentService.DeleteDocument(id);

            if (!success)
                return NotFound(new { message = "Document not found" });
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Cabinet", "Delete");
            return Ok(new { message = "Document deleted successfully" });
        }
    }
}
