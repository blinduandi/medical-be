using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using medical_be.Extensions;
using medical_be.Controllers.Base;
using AutoMapper;
using System.Security.Claims;

namespace medical_be.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<DocumentController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DocumentController(
            ApplicationDbContext context,
            IMapper mapper,
            IAuditService auditService,
            ILogger<DocumentController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _mapper = mapper;
            _auditService = auditService;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Get patient's medical documents
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [Authorize] // Allow all authenticated users, but add authorization logic inside
        public async Task<IActionResult> GetPatientDocuments(string patientId)
        {
            try
            {
                var currentUserId = User.GetUserId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Authorization check: Patients can only access their own documents
                if (userRole == "Patient" && patientId != currentUserId)
                {
                    return ForbiddenResponse("Patients can only access their own documents");
                }

                var documents = await _context.MedicalDocuments
                    .Where(d => d.PatientId == patientId)
                    .Include(d => d.UploadedBy)
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new MedicalDocumentDto
                    {
                        Id = d.Id,
                        PatientId = d.PatientId,
                        FileName = d.FileName,
                        DocumentType = d.DocumentType.ToString(),
                        Description = d.Description,
                        UploadedById = d.UploadedById,
                        UploadedByName = d.UploadedBy.FirstName + " " + d.UploadedBy.LastName,
                        FileSizeBytes = d.FileSizeBytes,
                        MimeType = d.MimeType ?? "application/octet-stream",
                        CreatedAt = d.CreatedAt
                    })
                    .ToListAsync();

                return SuccessResponse(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for patient: {PatientId}", patientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Get current patient's medical documents
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyDocuments()
        {
            try
            {
                var currentUserId = User.GetUserId();
                var documents = await _context.MedicalDocuments
                    .Where(d => d.PatientId == currentUserId)
                    .Include(d => d.UploadedBy)
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new MedicalDocumentDto
                    {
                        Id = d.Id,
                        PatientId = d.PatientId,
                        FileName = d.FileName,
                        DocumentType = d.DocumentType.ToString(),
                        Description = d.Description,
                        UploadedById = d.UploadedById,
                        UploadedByName = d.UploadedBy.FirstName + " " + d.UploadedBy.LastName,
                        FileSizeBytes = d.FileSizeBytes,
                        MimeType = d.MimeType ?? "application/octet-stream",
                        CreatedAt = d.CreatedAt
                    })
                    .ToListAsync();

                return SuccessResponse(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for current patient");
                return InternalServerErrorResponse("Internal server error");
            }
        }

        /// <summary>
        /// Upload medical document
        /// </summary>
        [HttpPost("upload")]
        [Authorize] // Allow all authenticated users
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentDTO uploadDto)
        {
            try
            {
                if (uploadDto.File == null || uploadDto.File.Length == 0)
                {
                    return ErrorResponse("No file provided");
                }

                // Validate file size (10MB limit)
                const long maxFileSize = 10 * 1024 * 1024;
                if (uploadDto.File.Length > maxFileSize)
                {
                    return ErrorResponse("File size exceeds 10MB limit");
                }

                // Validate file type
                var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/tiff", "application/dicom" };
                if (!allowedTypes.Contains(uploadDto.File.ContentType))
                {
                    return ErrorResponse("Invalid file type. Only PDF, JPEG, PNG, TIFF, and DICOM files are allowed");
                }

                // Validate document type
                if (string.IsNullOrWhiteSpace(uploadDto.DocumentType))
                {
                    return ErrorResponse("Document type is required");
                }

                if (!Enum.TryParse<DocumentType>(uploadDto.DocumentType, true, out var documentType))
                {
                    var validTypes = string.Join(", ", Enum.GetNames<DocumentType>());
                    return ErrorResponse($"Invalid document type. Valid types are: {validTypes}");
                }

                // Authorization check: Patients can only upload for themselves
                var currentUserId = User.GetUserId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (userRole == "Patient" && uploadDto.PatientId != currentUserId)
                {
                    return ForbiddenResponse("Patients can only upload documents for themselves");
                }
                
                // For Doctors and Admins, verify the patient exists
                if (userRole is "Doctor" or "Admin")
                {
                    var patientExists = await _context.Users.AnyAsync(u => u.Id == uploadDto.PatientId);
                    if (!patientExists)
                    {
                        return NotFoundResponse("Patient not found");
                    }
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", "medical-documents");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileExtension = Path.GetExtension(uploadDto.File.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadDto.File.CopyToAsync(stream);
                }

                // Create database record
                var document = new MedicalDocument
                {
                    PatientId = uploadDto.PatientId,
                    FileName = uploadDto.File.FileName,
                    StoredFileName = uniqueFileName,
                    DocumentType = documentType, // Use the validated enum value
                    Description = uploadDto.Description,
                    UploadedById = User.GetUserId(),
                    FileSizeBytes = uploadDto.File.Length,
                    MimeType = uploadDto.File.ContentType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MedicalDocuments.Add(document);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    User.GetUserId(),
                    "DocumentUploaded",
                    $"Uploaded document {uploadDto.File.FileName} for patient: {uploadDto.PatientId}",
                    "MedicalDocument",
                    document.Id,
                    Request.GetClientIpAddress()
                );

                // Create DTO manually since navigation properties are not loaded
                var currentUser = await _context.Users.FindAsync(User.GetUserId());
                var result = new MedicalDocumentDto
                {
                    Id = document.Id,
                    PatientId = document.PatientId,
                    VisitRecordId = document.VisitRecordId,
                    FileName = document.FileName,
                    FileType = document.FileType,
                    FilePath = document.StoredFileName,
                    FileSizeBytes = document.FileSizeBytes,
                    Description = document.Description,
                    DocumentType = document.DocumentType.ToString(),
                    UploadedById = document.UploadedById,
                    MimeType = document.MimeType,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt,
                    UploadedByName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : ""
                };
                
                return SuccessResponse(result, "Document uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for patient: {PatientId}", uploadDto.PatientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Upload medical document for current patient (simplified for patient self-upload)
        /// </summary>
        [HttpPost("upload/my")]
        [Authorize(Roles = "Patient")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMyDocument(IFormFile file, string documentType, string? description = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return ErrorResponse("No file provided");
                }

                // Validate file size (10MB limit)
                const long maxFileSize = 10 * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    return ErrorResponse("File size exceeds 10MB limit");
                }

                // Validate file type
                var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/tiff", "application/dicom" };
                if (!allowedTypes.Contains(file.ContentType))
                {
                    return ErrorResponse("Invalid file type. Only PDF, JPEG, PNG, TIFF, and DICOM files are allowed");
                }

                // Validate document type
                if (string.IsNullOrWhiteSpace(documentType))
                {
                    return ErrorResponse("Document type is required");
                }

                if (!Enum.TryParse<DocumentType>(documentType, true, out var parsedDocumentType))
                {
                    var validTypes = string.Join(", ", Enum.GetNames<DocumentType>());
                    return ErrorResponse($"Invalid document type. Valid types are: {validTypes}");
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", "medical-documents");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create database record using current user ID as patient ID
                var currentUserId = User.GetUserId();
                var document = new MedicalDocument
                {
                    PatientId = currentUserId,
                    FileName = file.FileName,
                    StoredFileName = uniqueFileName,
                    DocumentType = parsedDocumentType, // Use the validated enum value
                    Description = description,
                    UploadedById = currentUserId,
                    FileSizeBytes = file.Length,
                    MimeType = file.ContentType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MedicalDocuments.Add(document);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    currentUserId,
                    "DocumentUploaded",
                    $"Patient uploaded document {file.FileName}",
                    "MedicalDocument",
                    document.Id,
                    Request.GetClientIpAddress()
                );

                // Create DTO manually since navigation properties are not loaded
                var currentUser = await _context.Users.FindAsync(currentUserId);
                var result = new MedicalDocumentDto
                {
                    Id = document.Id,
                    PatientId = document.PatientId,
                    VisitRecordId = document.VisitRecordId,
                    FileName = document.FileName,
                    FileType = document.FileType,
                    FilePath = document.StoredFileName,
                    FileSizeBytes = document.FileSizeBytes,
                    Description = document.Description,
                    DocumentType = document.DocumentType.ToString(),
                    UploadedById = document.UploadedById,
                    MimeType = document.MimeType,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt,
                    UploadedByName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : ""
                };
                
                return SuccessResponse(result, "Document uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for current patient");
                return InternalServerErrorResponse("Internal server error");
            }
        }

        /// <summary>
        /// Upload medical document for a patient (Doctor/Admin only)
        /// </summary>
        [HttpPost("upload/patient/{patientId}")]
        [Authorize(Roles = "Doctor,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPatientDocument(string patientId, IFormFile file, string documentType, string? description = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return ErrorResponse("No file provided");
                }

                // Validate file size (10MB limit)
                const long maxFileSize = 10 * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    return ErrorResponse("File size exceeds 10MB limit");
                }

                // Validate file type
                var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/tiff", "application/dicom" };
                if (!allowedTypes.Contains(file.ContentType))
                {
                    return ErrorResponse("Invalid file type. Only PDF, JPEG, PNG, TIFF, and DICOM files are allowed");
                }

                // Validate document type
                if (string.IsNullOrWhiteSpace(documentType))
                {
                    return ErrorResponse("Document type is required");
                }

                if (!Enum.TryParse<DocumentType>(documentType, true, out var parsedDocumentType))
                {
                    var validTypes = string.Join(", ", Enum.GetNames<DocumentType>());
                    return ErrorResponse($"Invalid document type. Valid types are: {validTypes}");
                }

                // Verify the patient exists
                var patientExists = await _context.Users.AnyAsync(u => u.Id == patientId);
                if (!patientExists)
                {
                    return NotFoundResponse("Patient not found");
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", "medical-documents");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create database record
                var currentUserId = User.GetUserId();
                var document = new MedicalDocument
                {
                    PatientId = patientId,
                    FileName = file.FileName,
                    StoredFileName = uniqueFileName,
                    DocumentType = parsedDocumentType,
                    Description = description,
                    UploadedById = currentUserId,
                    FileSizeBytes = file.Length,
                    MimeType = file.ContentType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MedicalDocuments.Add(document);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    currentUserId,
                    "DocumentUploaded",
                    $"Doctor uploaded document {file.FileName} for patient: {patientId}",
                    "MedicalDocument",
                    document.Id,
                    Request.GetClientIpAddress()
                );

                // Create DTO manually since navigation properties are not loaded
                var currentUser = await _context.Users.FindAsync(currentUserId);
                var result = new MedicalDocumentDto
                {
                    Id = document.Id,
                    PatientId = document.PatientId,
                    VisitRecordId = document.VisitRecordId,
                    FileName = document.FileName,
                    FileType = document.FileType,
                    FilePath = document.StoredFileName,
                    FileSizeBytes = document.FileSizeBytes,
                    Description = document.Description,
                    DocumentType = document.DocumentType.ToString(),
                    UploadedById = document.UploadedById,
                    MimeType = document.MimeType,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt,
                    UploadedByName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : ""
                };
                
                return SuccessResponse(result, "Document uploaded successfully for patient");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for patient: {PatientId}", patientId);
                return InternalServerErrorResponse("Internal server error");
            }
        }

        /// <summary>
        /// Download medical document
        /// </summary>
        [HttpGet("{documentId}/download")]
        [Authorize] // Allow all authenticated users, but check ownership below
    public async Task<IActionResult> DownloadDocument(Guid documentId)
        {
            try
            {
                var document = await _context.MedicalDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return NotFoundResponse("Document not found");
                }

                // Authorization check: Patients can only download their own documents
                var currentUserId = User.GetUserId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (userRole == "Patient" && document.PatientId != currentUserId)
                {
                    return ForbiddenResponse("Patients can only download their own documents");
                }

                var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "medical-documents", document.StoredFileName);
                
                _logger.LogInformation("Attempting to download file: {FilePath}", filePath);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("File not found on disk: {FilePath}", filePath);
                    return NotFoundResponse("File not found on server");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                _logger.LogInformation("File read successfully, size: {Size} bytes", fileBytes.Length);

                // Audit log
                await _auditService.LogAuditAsync(
                    User.GetUserId(),
                    "DocumentDownloaded",
                    $"Downloaded document {document.FileName}",
                    "MedicalDocument",
                    document.Id,
                    Request.GetClientIpAddress()
                );

                // Ensure MimeType is not null or empty
                var mimeType = string.IsNullOrEmpty(document.MimeType) ? "application/octet-stream" : document.MimeType;
                
                // Set proper headers for file download
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{document.FileName}\"";
                Response.Headers["Content-Length"] = fileBytes.Length.ToString();
                
                return File(fileBytes, mimeType, document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document: {DocumentId}", documentId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Get document information (for debugging download issues)
        /// </summary>
        [HttpGet("{documentId}/info")]
        [Authorize]
        public async Task<IActionResult> GetDocumentInfo(Guid documentId)
        {
            try
            {
                var document = await _context.MedicalDocuments
                    .Include(d => d.UploadedBy)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return NotFoundResponse("Document not found");
                }

                // Authorization check: Patients can only access their own documents
                var currentUserId = User.GetUserId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (userRole == "Patient" && document.PatientId != currentUserId)
                {
                    return ForbiddenResponse("Patients can only access their own documents");
                }

                var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "medical-documents", document.StoredFileName);
                var fileExists = System.IO.File.Exists(filePath);

                var result = new
                {
                    Id = document.Id,
                    PatientId = document.PatientId,
                    FileName = document.FileName,
                    StoredFileName = document.StoredFileName,
                    DocumentType = document.DocumentType.ToString(),
                    Description = document.Description,
                    UploadedById = document.UploadedById,
                    UploadedByName = document.UploadedBy?.FirstName + " " + document.UploadedBy?.LastName,
                    FileSizeBytes = document.FileSizeBytes,
                    MimeType = document.MimeType,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt,
                    FileExistsOnDisk = fileExists,
                    FilePath = filePath
                };

                return SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document info: {DocumentId}", documentId);
                return InternalServerErrorResponse("Internal server error");
            }
        }

        /// <summary>
        /// Delete medical document
        /// </summary>
        [HttpDelete("{documentId}")]
        [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            try
            {
                var document = await _context.MedicalDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return NotFoundResponse("Document not found");
                }

                // Delete file from disk
                var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "medical-documents", document.StoredFileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Delete database record
                _context.MedicalDocuments.Remove(document);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    User.GetUserId(),
                    "DocumentDeleted",
                    $"Deleted document {document.FileName}",
                    "MedicalDocument",
                    document.Id,
                    Request.GetClientIpAddress()
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Update document description
        /// </summary>
        [HttpPut("{documentId}")]
        [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateDocument(Guid documentId, [FromBody] UpdateDocumentDTO updateDto)
        {
            try
            {
                var document = await _context.MedicalDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return NotFoundResponse("Document not found");
                }

                document.Description = updateDto.Description;
                document.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    User.GetUserId(),
                    "DocumentUpdated",
                    $"Updated document {document.FileName}",
                    "MedicalDocument",
                    document.Id,
                    Request.GetClientIpAddress()
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document: {DocumentId}", documentId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }
    }
}
