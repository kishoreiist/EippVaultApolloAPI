using EVWebApi.DTOs.Document;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVWebApi.DTOs.HR
{
    public class ConfigurationRequestDto
    {
        public string Name { get; set; }
        public int CollectionId { get; set; }
        
        public List<string> Emails { get; set; }

        public DateTime? ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(1);

        public string? Description { get; set; }
        
    }
    public class ConfigQueryParamsDto
    {
        [FromQuery(Name ="region")]
        public string? Region { get; set; }
       
    }

    public class ConfigQueryDetailDto
    {
        [FromQuery(Name = "status")]
        public string? Status { get; set; }
        [FromQuery(Name = "region")]
        public string? Region { get; set; }
        [FromQuery(Name = "type")]
        public string? Type { get; set; }
    }
    public class ConfigurationResponseDto
    {
        public int RequestId { get; set; }

        public int TotalEmails { get; set; }

        public int Success { get; set; }
        public int Failed { get; set; }

        public List<string> FailedEmailDetails { get; set; } = new List<string>();
    }

    public class UploadPageResponseDto
    {
        public int RequestId { get; set; }
        public int CandidateId { get; set; }
        public string Email { get; set; }
        public string? Name { get; set; }
        public string? DOB { get; set; }
        public string? Phone { get; set; }
        public string? Adhaar { get; set; }
        public string? PAN { get; set; }
        public string Designation { get; set; }
        public string CollectionName { get; set; }
        public string CollectionType { get; set; }
        public bool IsExternal { get; set; }
        public UploadStatusDto Status { get; set; }
        public List<DocumentTypeDto> Documents { get; set; }
    }

    public class DocumentTypeDto
    {
        public int DocumentTypeId { get; set; }
        public string DocType { get; set; }
        public bool Uploaded { get; set; }
    }

    public class UploadStatusDto
    {
        public string Status { get; set; }
        public int Total { get; set; }
        public int UploadedCount { get; set; }
        public int Pending { get; set; }
    }
    public class OnboardingDocsDto
    {
        public required string Token { get; set; }
        public required string Email { get; set; }
        public string? Name { get; set; }

        public string? AdhaarNo { get; set; }
        public string? PAN { get; set; }
        public string? Phone { get; set; }
        public string? EmployeeId { get; set; }
        public DateTime? Dob { get; set; }
        public List<UploadItemDto> Files { get; set; }
    }
    public class UploadItemDto
    {
        public int DocumentTypeId { get; set; }
        public IFormFile File { get; set; }
    }

    public class UploadResultDto
    {
        public string Status { get; set; }
        public List<DocumentTypeDto> Documents { get; set; }
    }


    public class ConfigListDto
    {
        public int ConfigId { get; set; }
        public string? Description { get; set; }
        public string CollectionName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Region { get; set; }
        public int TotalRecipients { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
    }


    public class ConfigRequestDetailsDto
    {
        public int ConfigId { get; set; }
        public string ConfigName { get; set; }
        public string? Description { get; set; }
        public string CollectionName { get; set; }
        public string CollectionType { get; set; }
        public string Region { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalDocs { get; set; }
        public List<RecipientDto> Recipients { get; set; }
    }

    public class RecipientDto
    {
        public int RecipientId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Adhaar { get; set; }
        public string PAN { get; set; }
        public DateTime? Dob { get; set; }
        public string Status { get; set; }
        public bool IsHired { get; set; }
        public bool IsLaptopRequestSent { get; set; }
        public DateTime? CompletedAt { get; set; }
        public SubmittedDocDto Submitted { get; set; }
        public PendingDocDto Pending { get; set; }

    }
    public class SubmittedDocDto
    {
        public int TotalSubmittedCount { get; set; }    
        public List<DocumentTypeDetailDto> Documents { get; set; }
    }
    public class DocumentTypeDetailDto
    {
        public int DocumentTypeId { get; set; }
        public string DocType { get; set; }
        public int FileId { get; set; }
        public string FilePath { get; set; }

    }
    public class DocumentTypeListDto
    {
        public int DocumentTypeId { get; set; }
        public string DocType { get; set; }
    }
    public class PendingDocDto
    {
        public int TotalPendingCount { get; set; }
        public List<DocumentTypeListDto> Documents { get; set; }

    }


    public class UploadFolderDto
    {
        public string SafeCandidateName { get; set; } = string.Empty;
        public string FinalFolderPath { get; set; } = string.Empty;
        public string OriginalFolderName { get; set; } = string.Empty;
    }
}
