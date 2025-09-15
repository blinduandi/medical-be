using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Blurhash.ImageSharp;

namespace medical_be.Services;

public class FileService : IFileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;
    private readonly string _thumbnailPath;
    private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

    public FileService(
        ApplicationDbContext context,
        ILogger<FileService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        
        _uploadPath = _configuration["FileStorage:UploadPath"] ?? "uploads";
        _thumbnailPath = _configuration["FileStorage:ThumbnailPath"] ?? "uploads/thumbnails";
        
        // Ensure directories exist
        Directory.CreateDirectory(_uploadPath);
        Directory.CreateDirectory(_thumbnailPath);
    }

    public async Task<FileResponseDto> UploadFileAsync(FileUploadDto dto, string userId)
    {
        try
        {
            // Get file type
            var fileType = await _context.FileTypes.FindAsync(dto.TypeId);
            if (fileType == null)
                throw new ArgumentException("Invalid file type");

            // Validate file
            if (!await ValidateFileAsync(dto.File, fileType))
                throw new ArgumentException("File validation failed");

            // Generate unique file name
            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var relativePath = Path.Combine(fileType.Category, DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"));
            var fullDirectoryPath = Path.Combine(_uploadPath, relativePath);
            
            Directory.CreateDirectory(fullDirectoryPath);
            
            var fullPath = Path.Combine(fullDirectoryPath, fileName);
            var dbPath = Path.Combine(relativePath, fileName).Replace('\\', '/');

            // Save file to disk
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            // Create file record
            var medicalFile = new MedicalFile
            {
                TypeId = dto.TypeId,
                Name = dto.File.FileName,
                Path = dbPath,
                Size = dto.File.Length,
                Extension = extension,
                MimeType = dto.File.ContentType,
                ModelType = dto.ModelType,
                ModelId = dto.ModelId,
                CreatedById = userId,
                Password = dto.Password,
                Label = dto.Label,
                IsTemporary = dto.IsTemporary
            };

            // Process image if applicable
            if (IsImageFile(extension))
            {
                await ProcessImageAsync(fullPath, medicalFile);
            }

            _context.MedicalFiles.Add(medicalFile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File uploaded successfully: {FileName} by user {UserId}", dto.File.FileName, userId);

            return await MapToResponseDto(medicalFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", dto.File.FileName);
            throw;
        }
    }

    public async Task<FileResponseDto?> GetFileAsync(Guid id)
    {
        var file = await _context.MedicalFiles
            .Include(f => f.Type)
            .Include(f => f.CreatedBy)
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);

        return file != null ? await MapToResponseDto(file) : null;
    }

    public async Task<IEnumerable<FileResponseDto>> GetFilesAsync(FileSearchDto searchDto)
    {
        var query = BuildFileQuery(searchDto);
        
        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var results = new List<FileResponseDto>();
        foreach (var file in files)
        {
            results.Add(await MapToResponseDto(file));
        }
        
        return results;
    }

    public async Task<(IEnumerable<FileResponseDto> Files, int TotalCount)> GetFilesPaginatedAsync(FileSearchDto searchDto)
    {
        var query = BuildFileQuery(searchDto);
        
        var totalCount = await query.CountAsync();
        
        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync();

        var results = new List<FileResponseDto>();
        foreach (var file in files)
        {
            results.Add(await MapToResponseDto(file));
        }
        
        return (results, totalCount);
    }

    public async Task<Stream?> GetFileStreamAsync(Guid id)
    {
        var file = await _context.MedicalFiles.FindAsync(id);
        if (file == null || file.DeletedAt != null)
            return null;

        var fullPath = Path.Combine(_uploadPath, file.Path);
        if (!File.Exists(fullPath))
            return null;

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }

    public async Task<bool> DeleteFileAsync(Guid id, string userId)
    {
        var file = await _context.MedicalFiles.FindAsync(id);
        if (file == null || file.IsDeleted)
            return false;

        // Soft delete
        file.DeletedAt = DateTime.UtcNow;
        file.DeletedById = userId;
        file.UpdatedAt = DateTime.UtcNow;
        file.UpdatedById = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("File soft deleted: {FileId} by user {UserId}", id, userId);
        return true;
    }

    public async Task<int> BulkDeleteFilesAsync(Guid[] fileIds, string userId)
    {
        var files = await _context.MedicalFiles
            .Where(f => fileIds.Contains(f.Id) && f.DeletedAt == null)
            .ToListAsync();

        var deleteTime = DateTime.UtcNow;
        
        foreach (var file in files)
        {
            file.DeletedAt = deleteTime;
            file.DeletedById = userId;
            file.UpdatedAt = deleteTime;
            file.UpdatedById = userId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("{Count} files bulk deleted by user {UserId}", files.Count, userId);
        return files.Count;
    }

    public async Task<FileResponseDto?> UpdateFileAsync(Guid id, FileUpdateDto dto, string userId)
    {
        var file = await _context.MedicalFiles
            .Include(f => f.Type)
            .Include(f => f.CreatedBy)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (file == null)
            return null;

        // Update fields
        if (dto.Label != null)
            file.Label = dto.Label;
        
        if (dto.Password != null)
            file.Password = dto.Password;
        
        if (dto.IsTemporary.HasValue)
            file.IsTemporary = dto.IsTemporary.Value;

        file.UpdatedAt = DateTime.UtcNow;
        file.UpdatedById = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("File updated: {FileId} by user {UserId}", id, userId);
        return await MapToResponseDto(file);
    }

    public async Task<IEnumerable<FileTypeResponseDto>> GetFileTypesAsync()
    {
        var fileTypes = await _context.FileTypes
            .Where(ft => ft.IsActive)
            .OrderBy(ft => ft.Category)
            .ThenBy(ft => ft.Name)
            .ToListAsync();

        return fileTypes.Select(MapToFileTypeDto);
    }

    public async Task<FileTypeResponseDto?> GetFileTypeAsync(int id)
    {
        var fileType = await _context.FileTypes.FindAsync(id);
        return fileType?.IsActive == true ? MapToFileTypeDto(fileType) : null;
    }

    public async Task<FileTypeResponseDto> CreateFileTypeAsync(FileTypeDto dto)
    {
        var fileType = new FileType
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            AllowedExtensions = JsonSerializer.Serialize(dto.AllowedExtensions),
            MaxSizeBytes = dto.MaxSizeBytes
        };

        _context.FileTypes.Add(fileType);
        await _context.SaveChangesAsync();

        _logger.LogInformation("File type created: {Name}", dto.Name);
        return MapToFileTypeDto(fileType);
    }

    public async Task<FileTypeResponseDto?> UpdateFileTypeAsync(int id, FileTypeDto dto)
    {
        var fileType = await _context.FileTypes.FindAsync(id);
        if (fileType == null || !fileType.IsActive)
            return null;

        fileType.Name = dto.Name;
        fileType.Description = dto.Description;
        fileType.Category = dto.Category;
        fileType.AllowedExtensions = JsonSerializer.Serialize(dto.AllowedExtensions);
        fileType.MaxSizeBytes = dto.MaxSizeBytes;
        fileType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("File type updated: {Id}", id);
        return MapToFileTypeDto(fileType);
    }

    public async Task<bool> DeleteFileTypeAsync(int id)
    {
        var fileType = await _context.FileTypes.FindAsync(id);
        if (fileType == null)
            return false;

        fileType.IsActive = false;
        fileType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("File type deactivated: {Id}", id);
        return true;
    }

    public Task<string> GenerateDownloadUrlAsync(Guid id)
    {
        // In a real application, you might generate signed URLs or temporary download links
        return Task.FromResult($"/api/files/{id}/download");
    }

    public async Task<string?> GenerateThumbnailAsync(Guid id)
    {
        var file = await _context.MedicalFiles.FindAsync(id);
        if (file == null || !IsImageFile(file.Extension))
            return null;

        var thumbnailFileName = $"{id}_thumb.jpg";
        var thumbnailPath = Path.Combine(_thumbnailPath, thumbnailFileName);

        if (File.Exists(thumbnailPath))
            return $"/api/files/{id}/thumbnail";

        try
        {
            var originalPath = Path.Combine(_uploadPath, file.Path);
            if (!File.Exists(originalPath))
                return null;

            using var image = await Image.LoadAsync(originalPath);
            
            // Resize to thumbnail size (200x200 max, maintaining aspect ratio)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(200, 200),
                Mode = ResizeMode.Max
            }));

            await image.SaveAsJpegAsync(thumbnailPath);
            return $"/api/files/{id}/thumbnail";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for file {FileId}", id);
            return null;
        }
    }

    public async Task CleanupTemporaryFilesAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-1); // Remove temp files older than 1 day

        var tempFiles = await _context.MedicalFiles
            .Where(f => f.IsTemporary && f.CreatedAt < cutoffDate)
            .ToListAsync();

        foreach (var file in tempFiles)
        {
            try
            {
                var fullPath = Path.Combine(_uploadPath, file.Path);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                _context.MedicalFiles.Remove(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up temporary file {FileId}", file.Id);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleaned up {Count} temporary files", tempFiles.Count);
    }

    public async Task<bool> ValidateFileAsync(IFormFile file, FileType fileType)
    {
        if (file.Length > fileType.MaxSizeBytes)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = JsonSerializer.Deserialize<string[]>(fileType.AllowedExtensions) ?? Array.Empty<string>();
        
        return allowedExtensions.Contains(extension.TrimStart('.'));
    }

    private IQueryable<MedicalFile> BuildFileQuery(FileSearchDto searchDto)
    {
        var query = _context.MedicalFiles
            .Include(f => f.Type)
            .Include(f => f.CreatedBy)
            .Where(f => f.DeletedAt == null);

        if (!string.IsNullOrEmpty(searchDto.ModelType))
            query = query.Where(f => f.ModelType == searchDto.ModelType);

        if (!string.IsNullOrEmpty(searchDto.ModelId))
            query = query.Where(f => f.ModelId == searchDto.ModelId);

        if (searchDto.TypeId.HasValue)
            query = query.Where(f => f.TypeId == searchDto.TypeId.Value);

        if (!string.IsNullOrEmpty(searchDto.Category))
            query = query.Where(f => f.Type.Category == searchDto.Category);

        if (searchDto.IsTemporary.HasValue)
            query = query.Where(f => f.IsTemporary == searchDto.IsTemporary.Value);

        if (searchDto.CreatedAfter.HasValue)
            query = query.Where(f => f.CreatedAt >= searchDto.CreatedAfter.Value);

        if (searchDto.CreatedBefore.HasValue)
            query = query.Where(f => f.CreatedAt <= searchDto.CreatedBefore.Value);

        return query;
    }

    private async Task<FileResponseDto> MapToResponseDto(MedicalFile file)
    {
        return new FileResponseDto
        {
            Id = file.Id,
            Name = file.Name,
            DisplayName = file.DisplayName,
            Size = file.Size,
            SizeFormatted = file.SizeFormatted,
            Extension = file.Extension,
            MimeType = file.MimeType,
            Label = file.Label,
            IsImage = file.IsImage,
            Width = file.Width,
            Height = file.Height,
            BlurHash = file.BlurHash,
            IsTemporary = file.IsTemporary,
            CreatedAt = file.CreatedAt,
            CreatedByName = file.CreatedBy != null ? $"{file.CreatedBy.FirstName} {file.CreatedBy.LastName}" : null,
            Type = MapToFileTypeDto(file.Type),
            DownloadUrl = await GenerateDownloadUrlAsync(file.Id),
            ThumbnailUrl = file.IsImage ? await GenerateThumbnailAsync(file.Id) : null
        };
    }

    private static FileTypeResponseDto MapToFileTypeDto(FileType fileType)
    {
        var allowedExtensions = JsonSerializer.Deserialize<string[]>(fileType.AllowedExtensions) ?? Array.Empty<string>();
        
        return new FileTypeResponseDto
        {
            Id = fileType.Id,
            Name = fileType.Name,
            Description = fileType.Description,
            Category = fileType.Category,
            AllowedExtensions = allowedExtensions,
            MaxSizeBytes = fileType.MaxSizeBytes,
            MaxSizeFormatted = FormatBytes(fileType.MaxSizeBytes)
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:N1} {suffixes[counter]}";
    }

    private bool IsImageFile(string? extension)
    {
        return !string.IsNullOrEmpty(extension) && _imageExtensions.Contains(extension.ToLowerInvariant());
    }

    private async Task ProcessImageAsync(string filePath, MedicalFile medicalFile)
    {
        try
        {
            using var image = await Image.LoadAsync(filePath);
            
            medicalFile.Width = image.Width;
            medicalFile.Height = image.Height;
            
            // Generate blur hash for image preview
            using var rgb24Image = image.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgb24>();
            medicalFile.BlurHash = Blurhasher.Encode(rgb24Image, 4, 4);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing image {FilePath}", filePath);
        }
    }
}
