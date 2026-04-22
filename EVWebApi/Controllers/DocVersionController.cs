using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Services;
using EVWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVWebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocVersionController : BaseController
    {
        private readonly IAuditLogService _auditlogservice;
        private readonly IDocVersionService _docVersionService;
        public DocVersionController(IAuditLogService auditlogservice, IDocVersionService docVersionService) 
        {
            _auditlogservice = auditlogservice;
            _docVersionService = docVersionService;
        }

        //-------------------versioning---------------

        [HttpGet("{docId}/versions")]
        public async Task<IActionResult> GetVersionsForDocument(int docId)
        {
            var version = await _docVersionService.GetDocumentVersionsAsync(docId);
            await _auditlogservice.LogAsync(CurrentUserId,CurrentUsername, "Version", "Versions Retrieved", docId.ToString());
            return Ok(new { data = version });
        }



        //-------------------locks----------------------

        [HttpPost("{docId}/lock")]
        public async Task<IActionResult> CreateLock(int docId)
        {
            try
            {
                var lockInfo = await _docVersionService.CreateDocumentLockAsync(docId, CurrentUserId);
                await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Lock", "Lock Created", lockInfo.DocumentId.ToString());
                return Ok(new
                {
                    documentId = lockInfo.DocumentId,
                    lockedBy = lockInfo.LockedBy,
                    lockedAt = lockInfo.LockedAt,
                    lockExpiry = lockInfo.LockExpiry
                });
            }
            catch (DocumentLockedException ex)
            {
                return Conflict(new
                {
                    message = "Document is locked by another user",
                    lockedBy = ex.LockedBy,
                    lockExpiry = ex.LockExpiry
                });
            }
        }

        [HttpDelete("{docId}/lock")]
        public async Task<IActionResult> ReleaseLock(int docId)
        {
       
            await _docVersionService.ReleaseLockAsync(docId, CurrentUserId);
            await _auditlogservice.LogAsync(CurrentUserId, CurrentUsername, "Lock", "Lock Released", docId.ToString());
            return NoContent();
        }
    }
}
