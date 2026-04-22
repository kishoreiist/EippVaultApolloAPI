using AutoMapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EVWebApi.Services
{
    public class DocVersionService: IDocVersionService
    {
        private readonly IUnitOfWork _uow;
        private readonly IDocVersionRepository _repo;
        private readonly IDocumentRepository _docRepo;
        private readonly IMapper _mapper;


        private readonly string _uploadRoot;
        private readonly string _tempRoot;
        private readonly string _storageRoot;
        private readonly string _clientName;
        private readonly string _versionRoot;
        public DocVersionService(IUnitOfWork uow, IDocVersionRepository repo, IMapper mapper,IDocumentRepository docRepo, IConfiguration config)
        {
            
            _uow = uow;
            _repo = repo;
            _mapper = mapper;
            _docRepo = docRepo;
            _uploadRoot = config["DocumentSettings:UploadPath"];
            _storageRoot = config["DocumentSettings:StorageRoot"];
            _tempRoot = config["DocumentSettings:TempPath"];
            _versionRoot = config["DocumentSettings:VersionPath"];
            _clientName = config["DocumentSettings:ClientName"];
        }   
        public async Task<List<DocumentVersion>> GetDocumentVersionsAsync(int id)
        {
            var document = await _uow.Documents.GetByIdAsync(id);

            if (document == null || document.Status == "archived")
                throw new NotFoundException($"Document with id {id} not found");

            var versions = await _uow.DocumentVersions.GetVersionsAsync(id);
            return versions;
        }

        public async Task<DocumentVersion> CreateVersionAsync(DocumentVersion doc, int versionNo)
        {

            var version = new DocumentVersion
            {
                DocumentId = doc.DocumentId,
                VersionNo = versionNo,
                UploadedAt = doc.UploadedAt,
                UploadedBy = doc.UploadedBy,

                FileName = doc.FileName,
                FilePath = doc.FilePath,

                InvoiceNumber = doc.InvoiceNumber ?? doc.InvoiceNumber,
                PoNumber = doc.PoNumber ?? doc.PoNumber,
                VendorNumber = doc.VendorNumber ?? doc.VendorNumber,
                EmployeeId = doc.EmployeeId ?? doc.EmployeeId,
                Name = doc.Name ?? doc.Name,
                ContactNumber = doc.ContactNumber ?? doc.ContactNumber,
                Designation = doc.Designation ?? doc.Designation,
                Department = doc.Department ?? doc.Department,
                Region = doc.Region ?? doc.Region,

                InvoiceDate = doc.InvoiceDate ?? doc.InvoiceDate,
                StatementDate = doc.StatementDate ?? doc.StatementDate,
                DOJ = doc.DOJ ?? doc.DOJ,
                DOB = doc.DOB ?? doc.DOB,
                Amount = doc.Amount ?? doc.Amount,
                GST = doc.GST ?? doc.GST,
                PaidAmount = doc.PaidAmount ?? doc.PaidAmount,
                CheckNumber = doc.CheckNumber ?? doc.CheckNumber,
                Status="active"
            };

            await _uow.DocumentVersions.AddAsync(version);
            await _uow.CompleteAsync();

            //doc.Version = version.VersionId;
            //await _uow.CompleteAsync();

            return version;
        }

        //-------------------------locks----------------------
        public async Task<DocumentLock> CreateDocumentLockAsync(int docId, int? userId)
        {
            var now = DateTime.UtcNow;

            var existingLock = await _repo.GetLockByDocumentIdAsync(docId);

            //Lock exists
            if (existingLock != null)
            {
                // Active lock by another user → BLOCK
                if (existingLock.LockedBy != userId && existingLock.LockExpiry > now)
                {
                    throw new DocumentLockedException(existingLock);
                }

                // Same user → refresh lock (important)
                if (existingLock.LockedBy == userId)
                {
                    existingLock.LockExpiry = now.AddMinutes(15);
                    await _uow.CompleteAsync();
                    return existingLock;
                }

                // Expired lock → remove
                if (existingLock.LockExpiry <= now)
                {
                    _repo.RemoveLock(existingLock);
                    await _uow.CompleteAsync();
                }
            }

            // Create new lock
            var newLock = new DocumentLock
            {
                DocumentId = docId,
                LockedBy = (int)userId,
                LockedAt = now,
                LockExpiry = now.AddMinutes(15)
            };

            try
            {
                await _repo.AddLockAsync(newLock);
                await _uow.CompleteAsync();
                return newLock;
            }
            catch (DbUpdateException)
            {
                //Another request inserted lock → re-fetch
                var retryLock = await _repo.GetLockByDocumentIdAsync(docId);

                if (retryLock != null &&
                    retryLock.LockedBy != userId &&
                    retryLock.LockExpiry > now)
                {
                    throw new DocumentLockedException(retryLock);
                }

                // If same user OR expired → safe fallback
                return retryLock!;
            }
        }

        public async Task<DocumentLock> CheckDocLockValidityAsync(int docId, int? userId)
        {
            var lockEntry = await _repo.GetLockByDocumentIdAsync(docId);

            // Cleanup expired lock
            if (lockEntry != null && lockEntry.LockExpiry <= DateTime.UtcNow)
            {
                _repo.RemoveLock(lockEntry);
                await _uow.CompleteAsync();
                lockEntry = null;
            }

            //No lock
            if (lockEntry == null)
            {
                lockEntry = await CreateDocumentLockAsync(docId, userId);
                return lockEntry;

                //throw new ConflictException("No active lock for User");
            }
                

            // other user locked
            if (lockEntry.LockedBy != userId)
                throw new ConflictException("Document is currently being edited by another user.");

            //Valid lock
            return lockEntry;
        }

        public async Task ReleaseLockAsync(int docId, int? userId)
        {
            var lockEntry = await _repo.GetLockByDocumentIdAsync(docId);

            if (lockEntry != null && lockEntry.LockedBy == userId)
            {
                _repo.RemoveLock(lockEntry);
                await _uow.CompleteAsync();
            }
        }



        public async Task RestoreVersionAsync(int docId, int versionId, int userId)
        {
            // Ensure lock
            await CheckDocLockValidityAsync(docId, userId);

            var version = await _repo.GetByIdAsync(versionId);
            if (version == null)
                throw new NotFoundException("Version not found");

            var document = await _docRepo.GetByIdAsync(docId);



            var docDbPath = document.FilePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());
            var versDbpath= version.FilePath.TrimStart('/', '\\').Replace("/", Path.DirectorySeparatorChar.ToString());

            var uploadRootTemplate = _uploadRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var versionTemp = _versionRoot
                .Replace("{StorageRoot}", _storageRoot)
                .Replace("{ClientName}", _clientName);

            var docfullPath = Path.Combine(uploadRootTemplate, docDbPath);
            var versionFullPath = Path.Combine(versionTemp, versDbpath);


            // 📂 Copy file
            File.Copy(versionFullPath, docfullPath, overwrite: true);

            // 🔢 Get next version number
            var latestVersion = await _docRepo.GetLatestVersion(docId) + 1;

            var versionEntity = _mapper.Map<DocumentVersion>(document);//need to check
            await CreateVersionAsync(versionEntity, latestVersion);
            await _uow.CompleteAsync();

          //  await _versionRepo.AddAsync(newVersion);
        }
    }
}
