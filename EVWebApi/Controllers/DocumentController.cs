using EVWebApi.DTOs.Document;
using EVWebApi.Exceptions;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using EVWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.CodeAnalysis.Host.HostWorkspaceServices;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EVWebApi.Controllers
{
    [Authorize]
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
            string filterDetails = dto.ToFilterLog("Index Details - ");
            try
            {
                var result = await _documentService.UploadDocument(dto, CurrentUserId);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Uploaded", result.FileName, result.CabinetId, filters: filterDetails);// need to pass index fileds as filters
                return Ok(result);
            }
            catch(Exception ex)
            {
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Upload Failed", ex.Message,dto.CabinetId, filters: filterDetails);
                return StatusCode(500, new
                {
                    Message = "Document upload failed",
                    Error = ex.Message
                });
            }
        }


        //Batch upload

        [HttpPost("batch_upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> BatchUploadDocument([FromForm] BatchUploadDTO dto)
        {
            try
            {
                // validation
                if (dto.MetadataFile == null || dto.MetadataFile.Length == 0)
                    return BadRequest("Metadata CSV file is required");

                if (dto.Files == null || dto.Files.Count == 0)
                    return BadRequest("At least one document file is required");

                if (!dto.MetadataFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Metadata file must be a CSV");

                var result = await _documentService.BatchUploadDocuments(dto, CurrentUserId);

                string filterDetails = result.ToFilterLog("Upload Status - ");

                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Batch Upload", null, dto.CabinetId, filters: filterDetails);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Upload Failed", ex.Message, dto.CabinetId);
                return StatusCode(500, new
                {
                    Message = "Document upload failed",
                    Error = ex.InnerException
                });
            }
        }

        // Get document by doc id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            var doc = await _documentService.GetDocument(id);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Viewed Document", doc.FileName,doc.CabinetId);
            return Ok(doc);
        }

        // Get all documents by CabinetId
        [HttpGet("cabinet/{cabinetId}")]
        public async Task<IActionResult> GetDocumentsByCabinetId(int cabinetId, [FromQuery] DocumentQueryParameters query)
        {
            var docs = await _documentService.GetDocumentsByCabinetId(cabinetId, query);
            string filterDetails = query.ToFilterLog();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Retrieved", null,cabinetId, null, filters: filterDetails);

            return Ok(docs);
        }

        // PDF Preview (stream)
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var fileStream = await _documentService.GetDocumentStream(id);
            if (fileStream == null) return NotFound();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "PDF View");

            return File(fileStream, "application/pdf", enableRangeProcessing: true);

        }


        //edit by doc id
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] UpdateDocumentDto dto)
        {
            var updated = await _documentService.UpdateDocumentAsync(id, dto);

            if (updated == null)
                return NotFound(new { message = "Document not found" });

            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Updated", updated.FileName,updated.CabinetId);

            return Ok(updated);
        }
        // DELETE DOC
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var (cabinetId, isSuccess) = await _documentService.DeleteDocument(id);

            if (!isSuccess)
                return NotFound(new { message = "Document not found" });
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Delete", null, cabinetId);
            return Ok(new { message = "Document deleted successfully" });
        }

        //File explorer 
        [HttpGet("fileexplorer/{cabinetid}")]
        public async Task<IActionResult> GetFileExplorerDocument(int cabinetid)
        {
            var files=await _documentService.GetFileExplorerDocumentAsync(cabinetid);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "File Explorer Accessed", null, cabinetid);
            return Ok(new { data = files });
        }


        //----------------------------NOTES----------------------------------

        //get notes by doc id
        [HttpGet("{documentId}/notes")]
        public async Task<IActionResult> GetNotesForDocument(int documentId)
        {
            var notes = await _documentService.GetDocumentWithNotesAsync(documentId);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Note", "Note Retrieved");
            //return Ok(notes);
            return Ok(new { data = notes });

        }

        //craete note
        [HttpPost("notes")]
        public async Task<IActionResult> CreateNote([FromBody] NoteCreateDto noteDto)
        {
            var note = await _documentService.CreateNoteAsync(noteDto, CurrentUsername);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Note", "Note Created", note.NoteText);
            return Ok(note); 
        }

        //edit note

        [HttpPut("notes/{noteId}")]
        public async Task<IActionResult> UpdateNote(int noteId, [FromBody] NoteUpdateDto noteDto)
        {
            var note = await _documentService.UpdateNoteAsync(noteDto);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Note", "Note Modified", note.NoteText);
            return Ok(note);
        }

        //delete note

        [HttpDelete("notes/{noteId}")]
        public async Task<IActionResult> DeleteNote(long noteId)
        {
            var noteText = await _documentService.DeleteNoteAsync(noteId);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Note", "Note Deleted",noteText);
            return NoContent();
        }
    }
}
