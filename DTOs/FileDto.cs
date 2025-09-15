using System.ComponentModel.DataAnnotations;

namespace medical_be.DTOs;

public class FileUploadDto
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    public int TypeId { get; set; }

    public string? Label { get; set; }

    public string? ModelType { get; set; }

    public string? ModelId { get; set; }

    public string? Password { get; set; }

    public bool IsTemporary { get; set; } = false;
}

public class ProfilePhotoUploadDto
{
    [Required]
    public IFormFile File { get; set; } = null!;
}

public class FileResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public string? MimeType { get; set; }
    public string? Label { get; set; }
    public bool IsImage { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? BlurHash { get; set; }
    public bool IsTemporary { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public FileTypeResponseDto Type { get; set; } = null!;
    public string DownloadUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

public class FileTypeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public long MaxSizeBytes { get; set; }
    public string MaxSizeFormatted { get; set; } = string.Empty;
}

public class FileTypeDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

    public long MaxSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default
}

public class FileSearchDto
{
    public string? ModelType { get; set; }
    public string? ModelId { get; set; }
    public int? TypeId { get; set; }
    public string? Category { get; set; }
    public bool? IsTemporary { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class FileBulkDeleteDto
{
    [Required]
    public Guid[] FileIds { get; set; } = Array.Empty<Guid>();
}

public class FileUpdateDto
{
    public string? Label { get; set; }
    public string? Password { get; set; }
    public bool? IsTemporary { get; set; }
}
