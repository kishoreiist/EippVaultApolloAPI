using EVWebApi.DTOs.Document;
using EVWebApi.Helpers;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace EVWebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : BaseController
    {
        private readonly IDocumentService _documentService;
        private readonly IAuditLogService _auditlogservice;
        private readonly IDocumentRepository _documentRepo;
        private readonly ILogger<DocumentController> _logger;
        public DocumentController(IDocumentService documentService, IAuditLogService auditLogService, IDocumentRepository documentRepo,
            ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _auditlogservice = auditLogService;
            _documentRepo = documentRepo;
            _logger = logger;
        }

        //Upload Document
        [HttpPost("upload")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto dto)
        {
            string filterDetails = dto.ToFilterLog("Index Details - ");
            try
            {
                var result = await _documentService.UploadDocumentChunks(dto, CurrentUserId,CurrentUsername,CurrentUserFullname);

                //CASE 1: Intermediate chunk (result is null)
                if (result == null)
                {
                    return Ok(new
                    {
                        message = "Chunk uploaded successfully",
                        chunkIndex = dto.ChunkIndex,
                        totalChunks = dto.TotalChunks
                    });
                }
                // CASE 2: Last chunk (file fully processed)
                if (dto.TotalChunks == null || dto.ChunkIndex == dto.TotalChunks - 1)
                {
                    _logger.LogInformation("File uploaded: {FileName} Size: {FileSize} UserId: {UserId}", result.FileName, dto.File.Length, CurrentUserId);
                    await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Uploaded", result.FileName, result.CabinetId, filters: filterDetails);
                }
                //CASE 3: Duplicate handling
                if (result.Actions != null)
                {
                    return StatusCode(409, new
                    {
                        message = "Duplicate found",
                        requiresAction = true,
                        documentId = result.DocumentId,
                        actions = result.Actions
                    });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Upload Failed", ex.Message, dto.CabinetId, filters: filterDetails);
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
                    return BadRequest("Metadata file is required");

                if (dto.Files == null || dto.Files.Count == 0)
                    return BadRequest("At least one document file is required");

                var result = await _documentService.BatchUploadDocuments(dto, CurrentUserId, CurrentUsername, CurrentUserFullname);

                string filterDetails = result.ToFilterLog("Upload Status - ");

                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Batch Upload", null, dto.CabinetId, filters: filterDetails);
                //if (result.Success == 0 && result.Failed > 0)
                //{
                //    return UnprocessableEntity(result);

                //}
                //if (result.Success > 0 && result.Failed > 0)
                //{
                //    return StatusCode(207, result); // Partial success
                //}

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Upload Failed", ex.Message, dto.CabinetId);
                return StatusCode(500, new
                {
                    Message = "Document upload failed",
                    Error = ex.Message
                });
            }
        }

        // Get document by doc id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            var doc = await _documentService.GetDocument(id);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Viewed Document", doc.FileName, doc.CabinetId);
            return Ok(doc);
        }

        // Get all documents by CabinetId
        [HttpGet("cabinet/{cabinetId}")]
        public async Task<IActionResult> GetDocumentsByCabinetId(int cabinetId, [FromQuery] DocumentQueryParameters query)
        {
            var docs = await _documentService.GetDocumentsByCabinetId(cabinetId, query);
            string filterDetails = query.ToFilterLog();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Retrieved", null, cabinetId, null, filters: filterDetails);

            return Ok(docs);
        }

        //get all document based on cabinetid grouped by doc type
        [HttpGet("grouped/{cabinetId}")]
        public async Task<IActionResult> GetGroupedDocumentsByCabinetId(int cabinetId, [FromQuery] DocumentQueryParameters query)
        {
            try
            {

                var docs = await _documentService.GetGroupedDocuments(cabinetId, query);
                string filterDetails = query.ToFilterLog();
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Retrieved", null, cabinetId, null, filters: filterDetails);

                return Ok(docs);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Document grouping failed",
                    Error = ex.Message
                });
            }
        }


        // PDF Preview (stream)

        [HttpPost("preview")]
        public async Task<IActionResult> PreviewDocument([FromBody] DocumentRequestDto dto)
        {
            try
            {
                var result = await _documentService.GetDocumentStream(dto);
                if (result == null) return NotFound();
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document View");

                var contentType = FileContentTypeDetectHelper.GetContentType(result.FilePath);

                return File(result.Stream, contentType, enableRangeProcessing: true);
            }
            catch
            {
                throw;
            }

        }

        // Download

        [HttpPost("download")]
        public async Task<IActionResult> DownloadDocument([FromBody] DocumentRequestDto dto)
        {
            var download = await _documentService.GetDocumentStream(dto);
            if (download == null) return NotFound();
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Download");

            var contentType = FileContentTypeDetectHelper.GetContentType(download.FileName);

            return File(download.Stream, contentType, download.FileName, enableRangeProcessing: true);
        }



        //edit by doc id
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] UpdateDocumentDto dto)
        {
            try
            {

            var updated = await _documentService.UpdateDocumentAsync(id, dto, CurrentUserId);

            if (updated == null)
                return NotFound(new { message = "Document not found" });

            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Updated", updated.FileName, updated.CabinetId);

            return Ok(updated);

            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Document updated failed",
                    Error = ex.Message
                });
            }
        }
        // SINGLE DELETE DOC
        [HttpDelete]
        public async Task<IActionResult> DeleteDocument([FromQuery] DocumentRequestDto dto)
        {
            var (cabinetId, isSuccess) = await _documentService.DeleteDocument(dto);

            if (!isSuccess)
                return NotFound(new { message = "Document deletion failed" });
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Document Delete", dto.Id.ToString(), cabinetId);
            return Ok(new { message = "Document deleted successfully" });
        }

        // BATCH DELETE DOCS
        [HttpPost("batch_delete")]
        public async Task<IActionResult> BatchDeleteDocuments([FromBody] ExportDto dto)
        {
            var result = await _documentService.DeleteMultipleDocuments(dto);
            string filterDetails = result.ToFilterLog("Summary - ");

            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Batch Document Delete", null, dto.CabinetId, null, filters: filterDetails);

            if (result.Success == 0)
                return NotFound(new { message = "No documents were deleted." });

            return Ok(result);
        }

        //File explorer 
        [HttpGet("fileexplorer/{cabinetid}")]
        public async Task<IActionResult> GetFileExplorerDocument(int cabinetid)
        {
            var files = await _documentService.GetFileExplorerDocumentAsync(cabinetid);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "File Explorer Accessed", null, cabinetid);
            return Ok(new { data = files });
        }

        //GET DOC TYPES
        [HttpGet("doctype")]
        public async Task<IActionResult> GetDocType()
        {
            var docTypes = await _documentService.GetDocTypeAsync();
            if (docTypes == null) return NotFound("Document type not found");
            return Ok(new
            {
                data = docTypes
            });
        }

        // Merge and Preview Documents

        [HttpPost("merge_pdf")]
        public async Task<IActionResult> ExportMergedDocuments([FromBody] ExportDto dto)
        {
            if (dto == null || dto.Documents == null || !dto.Documents.Any())
                return BadRequest("No document IDs provided");

            var mergedStream = await _documentService.GetMergedDocumentStream(dto);
            if (mergedStream == null)
                return NotFound();

            await _auditlogservice.LogAsync(
                CurrentUserId,
                CurrentUsername,
                "Document",
                "Export Merged PDF", null, dto.CabinetId
            );

            mergedStream.Position = 0;

            return File(
                mergedStream,
                "application/pdf",
                "merged.pdf",
                enableRangeProcessing: true
            );
        }


        //Export to zip file


        [HttpPost("export_zip")]
        public async Task<IActionResult> ExportZip([FromBody] ExportDto dto)
        {
            if (dto == null || dto.Documents == null || !dto.Documents.Any())
                return BadRequest("No document IDs provided");
            var zip_stream = await _documentService.GetZIPFile(dto);
            if (zip_stream.ZipStream == null)
                return NotFound();

            await _auditlogservice.LogAsync(
                CurrentUserId,
                CurrentUsername,
                "Document",
                "Export File", null, dto.CabinetId
            );


            return File(zip_stream.ZipStream, "application/zip", zip_stream.ZipFileName);

        }

        //export to excel
        [HttpPost("export_excel")]
        public async Task<IActionResult> ExportExcel([FromBody] ExportExcelDocDto dto)
        {

            var excel = await _documentService.GetExportExcel(dto);

            await _auditlogservice.LogAsync(
                CurrentUserId,
                CurrentUsername,
                "Document",
                "Export File", null, dto.CabinetId
            );
            return File(
                excel.Excel,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                excel.FileName);
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
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Note", "Note Deleted", noteText);
            return NoContent();
        }

        //excel patch
        [HttpPatch("excel_patch")]
        public async Task<IActionResult> PatchExcel([FromBody] ExcelPatchRequestDto dto)
        {
            if (dto.Changes == null || !dto.Changes.Any())
                return BadRequest("No changes provided");
            try
            {
                var result = await _documentService.ApplyExcelPatchAsync(dto, CurrentUserId);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Excel Patch", $"{dto.DocumentId}", dto.CabinetId);
                if (result.Success == 0 && result.Failed > 0)
                {
                    return UnprocessableEntity(result);

                }
                if (result.Success > 0 && result.Failed > 0)
                {
                    return StatusCode(207, result); // Partial success
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the Excel document",
                    error = ex.Message
                });
            }
        }

        //auto sugesstion
        [HttpGet("auto_suggestions")]
        public async Task<IActionResult> GetAutoSuggestions([FromQuery] AutoSuggestionRequestDto dto)
        {
            try
            {
                var result = await _documentService.GetSuggestionsAsync(dto);
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching auto suggestions",
                    error = ex.Message
                });
            }
        }

        //auto fill

        [HttpGet("auto-fill")]
        public async Task<IActionResult> GetAutoFill([FromQuery] AutoFillRequestDto dto)
        {
            try
            {
                var result = await _documentService.GetAutoFillAsync(dto);
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching auto suggestions",
                    error = ex.Message
                });
            }

        }

        //split document
        [HttpPost("split")]
        public async Task<IActionResult> SplitAndExtractPdf([FromBody] SplitAndExtractPdfDto dto)
        {
            if (dto.FromPage <= 0 || dto.ToPage <= 0 || dto.FromPage > dto.ToPage)
            {
                return BadRequest("Invalid page range.");
            }

            try
            {
                var result = await _documentService.SplitAndExtractPdfAsync(dto, CurrentUserId, CurrentUsername, CurrentUserFullname);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "PDF SPLIT", $"{dto.FromPage} - {dto.ToPage} into {dto.DocumentType} document Type", dto.CabinetId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while splitting the document.",
                    Error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        //------doc downlod link---------------
        //get all records
        [HttpGet("download_records")]
        public async Task<IActionResult> GetDocumentDownload()
        {
            try
            {
                var downloadLink = await _documentService.GetAllDocumentForDownloadAsync(CurrentUserId);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Download items Generated");
                //return Ok(new { data=downloadLink });


                if (!downloadLink.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "No documents assigned for download.",
                        data = new List<DocDownloadGetDTO>()
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Documents fetched successfully.",
                    data = downloadLink
                });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while generating the download link",
                    error = ex.Message
                });
            }

        }

        //download encrypted one
        [HttpPost("download_record")]
        public async Task<IActionResult> DownloadEncryptedDocument([FromBody] DocumentRequestDto dto)
        {
            try
            {
                var result = await _documentService.GenerateProtectedDownloadAsync(dto, CurrentUserId);
                //var stream = new FileStream(result.ProtectedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 16 * 1024 * 1024, useAsync: true);
                // var contentType = FileContentTypeDetectHelper.GetContentType(result.ProtectedFilePath);
                /// return File(stream, "application/octet-stream", contentType, enableRangeProcessing: true);

                if (result == null) return NotFound();
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Download-External");

                var contentType = FileContentTypeDetectHelper.GetContentType(result.FilePath);

                return File(result.Stream, contentType, fileDownloadName: result.FileName, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Download-External Failed", ex.Message);
                return StatusCode(500, new
                {
                    message = "An error occurred while downloading the document",
                    error = ex.Message
                });
            }

        }


        ////request download access
        //[HttpPost("request_access/{documentId}")]



        //excel open

        //--------------get sheetname-----------------
        [HttpGet("sheets")]
        public async Task<IActionResult> GetSheetNames([FromQuery] DocumentRequestDto dto)
        {
            try
            {
                var sheets = await _documentService.GetExcelSheetNamesAsync(dto);
                return Ok(sheets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while opening the Excel sheet",
                    error = ex.Message
                });
            }

        }


        [HttpPost("sheets/open/")]
        public async Task<IActionResult> OpenExcelSheet([FromBody] DocumentExcelOpenDTO dto)
        {
            string filterDetails = dto.ToFilterLog("");
            try
            {
                var json = await _documentService.OpenExcelSheetAsync(dto);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Excel open", null, null, filters: filterDetails);
                return Content(json, "application/json");
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Document", "Excel open fail", null, null, filters: filterDetails);
                return StatusCode(500, new
                {
                    message = "An error occurred while opening the Excel sheet",
                    error = ex.Message
                });
            }

        }

        [HttpGet("manufacture_details")]
        public async Task<IActionResult> GetManufactureDetails()
        {
            try
            {
                var details = await _documentService.GetManufactureDetailsAsync();
                if (details == null) return NotFound();
                return Ok(new {data= details});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching manufacture details",
                    error = ex.Message
                });
            }

        }
    }

}
