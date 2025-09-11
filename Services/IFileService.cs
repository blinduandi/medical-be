using medical_be.Models;
using medical_be.DTOs;

namespace medical_be.Services;

public interface IFileService
{
    Task<FileResponseDto> UploadFileAsync(FileUploadDto dto, string userId);
    Task<FileResponseDto?> GetFileAsync(Guid id);
    Task<IEnumerable<FileResponseDto>> GetFilesAsync(FileSearchDto searchDto);
    Task<(IEnumerable<FileResponseDto> Files, int TotalCount)> GetFilesPaginatedAsync(FileSearchDto searchDto);
    Task<Stream?> GetFileStreamAsync(Guid id);
    Task<bool> DeleteFileAsync(Guid id, string userId);
    Task<int> BulkDeleteFilesAsync(Guid[] fileIds, string userId);
    Task<FileResponseDto?> UpdateFileAsync(Guid id, FileUpdateDto dto, string userId);
    Task<IEnumerable<FileTypeResponseDto>> GetFileTypesAsync();
    Task<FileTypeResponseDto?> GetFileTypeAsync(int id);
    Task<FileTypeResponseDto> CreateFileTypeAsync(FileTypeDto dto);
    Task<FileTypeResponseDto?> UpdateFileTypeAsync(int id, FileTypeDto dto);
    Task<bool> DeleteFileTypeAsync(int id);
    Task<string> GenerateDownloadUrlAsync(Guid id);
    Task<string?> GenerateThumbnailAsync(Guid id);
    Task CleanupTemporaryFilesAsync();
    Task<bool> ValidateFileAsync(IFormFile file, FileType fileType);
}
