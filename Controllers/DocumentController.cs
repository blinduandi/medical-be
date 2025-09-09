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
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> GetPatientDocuments(string patientId)
        {
            try
            {
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
                        MimeType = d.MimeType,
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
        /// Upload medical document
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Roles = "Doctor,Admin")]
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
                    DocumentType = Enum.Parse<DocumentType>(uploadDto.DocumentType),
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

                var result = _mapper.Map<MedicalDocumentDto>(document);
                return CreatedAtAction(nameof(GetPatientDocuments), new { patientId = uploadDto.PatientId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for patient: {PatientId}", uploadDto.PatientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Download medical document
        /// </summary>
        [HttpGet("{documentId}/download")]
        [Authorize(Roles = "Doctor,Admin")]
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

                var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "medical-documents", document.StoredFileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFoundResponse("File not found on server");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                // Audit log
                await _auditService.LogAuditAsync(
                    User.GetUserId(),
                    "DocumentDownloaded",
                    $"Downloaded document {document.FileName}",
                    "MedicalDocument",
                    document.Id,
                    Request.GetClientIpAddress()
                );

                return File(fileBytes, document.MimeType, document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document: {DocumentId}", documentId);
                return InternalServerErrorResponse( "Internal server error");
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
