using EVWebApi.DTOs;
using EVWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using EVWebApi.Interfaces.Services;

namespace EVWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        // Upload Document
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto dto)
        {
            var result = await _documentService.UploadDocument(dto);
            return Ok(result);
        }

        // Get document metadata
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            var doc = await _documentService.GetDocument(id);
            return Ok(doc);
        }

        // PDF Preview (stream)
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var fileStream = await _documentService.GetDocumentStream(id);
            if (fileStream == null) return NotFound();

            return File(fileStream, "application/pdf");
        }

        // Download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var download = await _documentService.GetDocumentForDownload(id);
            if (download == null) return NotFound();

            return File(download.Stream, "application/octet-stream", download.FileName);
        }

        // Archive
        [HttpPut("{id}/archive")]
        public async Task<IActionResult> ArchiveDocument(int id)
        {
            await _documentService.ArchiveDocument(id);
            return Ok(new { message = "Document archived" });
        }

        // Restore
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> RestoreDocument(int id)
        {
            await _documentService.RestoreDocument(id);
            return Ok(new { message = "Document restored" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            bool success = await _documentService.DeleteDocument(id);

            if (!success)
                return NotFound(new { message = "Document not found" });

            return Ok(new { message = "Document deleted successfully" });
        }
    }
}
