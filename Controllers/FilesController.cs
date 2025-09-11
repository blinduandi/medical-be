using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using medical_be.Controllers.Base;
using medical_be.Services;
using medical_be.DTOs;
using System.Security.Claims;

namespace medical_be.Controllers;

[Route("api/[controller]")]
[Authorize]
public class FilesController : BaseApiController
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new file
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] FileUploadDto dto)
    {
        try
        {
            var modelValidation = ValidateModel();
            if (modelValidation != null) return modelValidation;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated");

            var result = await _fileService.UploadFileAsync(dto, userId);
            return SuccessResponse(result, "File uploaded successfully");
        }
        catch (ArgumentException ex)
        {
            return ValidationErrorResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return InternalServerErrorResponse("Failed to upload file");
        }
    }

    /// <summary>
    /// Get file by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        try
        {
            var file = await _fileService.GetFileAsync(id);
            if (file == null)
                return NotFoundResponse("File not found");

            return SuccessResponse(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {FileId}", id);
            return InternalServerErrorResponse("Failed to retrieve file");
        }
    }

    /// <summary>
    /// Search files with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFiles([FromQuery] FileSearchDto searchDto)
    {
        try
        {
            var (files, totalCount) = await _fileService.GetFilesPaginatedAsync(searchDto);
            return PaginatedResponse(files, searchDto.Page, searchDto.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching files");
            return InternalServerErrorResponse("Failed to search files");
        }
    }

    /// <summary>
    /// Download file
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        try
        {
            var file = await _fileService.GetFileAsync(id);
            if (file == null)
                return NotFoundResponse("File not found");

            var stream = await _fileService.GetFileStreamAsync(id);
            if (stream == null)
                return NotFoundResponse("File content not found");

            return File(stream, file.MimeType ?? "application/octet-stream", file.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", id);
            return InternalServerErrorResponse("Failed to download file");
        }
    }

    /// <summary>
    /// Get file thumbnail (for images)
    /// </summary>
    [HttpGet("{id:guid}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(Guid id)
    {
        try
        {
            var file = await _fileService.GetFileAsync(id);
            if (file == null || !file.IsImage)
                return NotFoundResponse("Image not found");

            var thumbnailUrl = await _fileService.GenerateThumbnailAsync(id);
            if (thumbnailUrl == null)
                return NotFoundResponse("Thumbnail not available");

            // In a real implementation, you would serve the actual thumbnail file
            // For now, return the thumbnail URL
            return SuccessResponse(new { ThumbnailUrl = thumbnailUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for file {FileId}", id);
            return InternalServerErrorResponse("Failed to generate thumbnail");
        }
    }

    /// <summary>
    /// Update file metadata
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateFile(Guid id, [FromBody] FileUpdateDto dto)
    {
        try
        {
            var modelValidation = ValidateModel();
            if (modelValidation != null) return modelValidation;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated");

            var result = await _fileService.UpdateFileAsync(id, dto, userId);
            if (result == null)
                return NotFoundResponse("File not found");

            return SuccessResponse(result, "File updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file {FileId}", id);
            return InternalServerErrorResponse("Failed to update file");
        }
    }

    /// <summary>
    /// Delete file (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated");

            var success = await _fileService.DeleteFileAsync(id, userId);
            if (!success)
                return NotFoundResponse("File not found");

            return SuccessResponse(null, "File deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", id);
            return InternalServerErrorResponse("Failed to delete file");
        }
    }

    /// <summary>
    /// Bulk delete files
    /// </summary>
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDeleteFiles([FromBody] FileBulkDeleteDto dto)
    {
        try
        {
            var modelValidation = ValidateModel();
            if (modelValidation != null) return modelValidation;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated");

            var deletedCount = await _fileService.BulkDeleteFilesAsync(dto.FileIds, userId);
            return SuccessResponse(new { DeletedCount = deletedCount }, $"{deletedCount} files deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting files");
            return InternalServerErrorResponse("Failed to delete files");
        }
    }

    /// <summary>
    /// Get files for a specific entity (patient, visit record, etc.)
    /// </summary>
    [HttpGet("entity/{modelType}/{modelId}")]
    public async Task<IActionResult> GetEntityFiles(string modelType, string modelId, [FromQuery] int? typeId)
    {
        try
        {
            var searchDto = new FileSearchDto
            {
                ModelType = modelType,
                ModelId = modelId,
                TypeId = typeId,
                PageSize = 100 // Get all files for the entity
            };

            var files = await _fileService.GetFilesAsync(searchDto);
            return SuccessResponse(files, "Entity files retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files for entity {ModelType}/{ModelId}", modelType, modelId);
            return InternalServerErrorResponse("Failed to retrieve entity files");
        }
    }

    /// <summary>
    /// Get file types
    /// </summary>
    [HttpGet("types")]
    public async Task<IActionResult> GetFileTypes()
    {
        try
        {
            var fileTypes = await _fileService.GetFileTypesAsync();
            return SuccessResponse(fileTypes, "File types retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file types");
            return InternalServerErrorResponse("Failed to retrieve file types");
        }
    }

    /// <summary>
    /// Create file type (Admin only)
    /// </summary>
    [HttpPost("types")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateFileType([FromBody] FileTypeDto dto)
    {
        try
        {
            var modelValidation = ValidateModel();
            if (modelValidation != null) return modelValidation;

            var result = await _fileService.CreateFileTypeAsync(dto);
            return SuccessResponse(result, "File type created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file type");
            return InternalServerErrorResponse("Failed to create file type");
        }
    }

    /// <summary>
    /// Update file type (Admin only)
    /// </summary>
    [HttpPut("types/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFileType(int id, [FromBody] FileTypeDto dto)
    {
        try
        {
            var modelValidation = ValidateModel();
            if (modelValidation != null) return modelValidation;

            var result = await _fileService.UpdateFileTypeAsync(id, dto);
            if (result == null)
                return NotFoundResponse("File type not found");

            return SuccessResponse(result, "File type updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file type {Id}", id);
            return InternalServerErrorResponse("Failed to update file type");
        }
    }

    /// <summary>
    /// Delete file type (Admin only)
    /// </summary>
    [HttpDelete("types/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFileType(int id)
    {
        try
        {
            var success = await _fileService.DeleteFileTypeAsync(id);
            if (!success)
                return NotFoundResponse("File type not found");

            return SuccessResponse(null, "File type deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file type {Id}", id);
            return InternalServerErrorResponse("Failed to delete file type");
        }
    }

    /// <summary>
    /// Get current user's profile picture
    /// </summary>
    [HttpGet("profile-picture")]
    public async Task<IActionResult> GetProfilePicture()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated");

            var searchDto = new FileSearchDto
            {
                ModelType = "User",
                ModelId = userId,
                Category = "ProfilePhoto",
                PageSize = 1
            };

            var files = await _fileService.GetFilesAsync(searchDto);
            var profilePicture = files.FirstOrDefault();

            if (profilePicture == null)
                return NotFoundResponse("Profile picture not found");

            return SuccessResponse(profilePicture, "Profile picture retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile picture");
            return InternalServerErrorResponse("Failed to retrieve profile picture");
        }
    }

    /// <summary>
    /// Upload/Update current user's profile picture
    /// </summary>
    [HttpPost("profile-picture")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not authenticated");

            if (file == null)
                return ValidationErrorResponse("No file provided");

            // Get the Profile Photo file type (assuming it's ID 1 from seeding)
            var fileTypes = await _fileService.GetFileTypesAsync();
            var profilePhotoType = fileTypes.FirstOrDefault(ft => ft.Category == "ProfilePhoto");
            
            if (profilePhotoType == null)
                return ValidationErrorResponse("Profile photo file type not configured");

            // Check if user already has a profile picture and delete it
            var existingFiles = await _fileService.GetFilesAsync(new FileSearchDto
            {
                ModelType = "User",
                ModelId = userId,
                Category = "ProfilePhoto"
            });

            foreach (var existingFile in existingFiles)
            {
                await _fileService.DeleteFileAsync(existingFile.Id, userId);
            }

            // Upload new profile picture
            var uploadDto = new FileUploadDto
            {
                File = file,
                TypeId = profilePhotoType.Id,
                ModelType = "User",
                ModelId = userId,
                Label = "Profile Picture"
            };

            var result = await _fileService.UploadFileAsync(uploadDto, userId);
            return SuccessResponse(result, "Profile picture uploaded successfully");
        }
        catch (ArgumentException ex)
        {
            return ValidationErrorResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture");
            return InternalServerErrorResponse("Failed to upload profile picture");
        }
    }

    /// <summary>
    /// Get user's profile picture by user ID (for doctors viewing patients)
    /// </summary>
    [HttpGet("profile-picture/{userId}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> GetUserProfilePicture(string userId)
    {
        try
        {
            var searchDto = new FileSearchDto
            {
                ModelType = "User",
                ModelId = userId,
                Category = "ProfilePhoto",
                PageSize = 1
            };

            var files = await _fileService.GetFilesAsync(searchDto);
            var profilePicture = files.FirstOrDefault();

            if (profilePicture == null)
                return NotFoundResponse("Profile picture not found");

            return SuccessResponse(profilePicture, "Profile picture retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile picture");
            return InternalServerErrorResponse("Failed to retrieve profile picture");
        }
    }

    /// <summary>
    /// Cleanup temporary files (Admin only)
    /// </summary>
    [HttpPost("cleanup-temp")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CleanupTemporaryFiles()
    {
        try
        {
            await _fileService.CleanupTemporaryFilesAsync();
            return SuccessResponse(null, "Temporary files cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up temporary files");
            return InternalServerErrorResponse("Failed to cleanup temporary files");
        }
    }
}
