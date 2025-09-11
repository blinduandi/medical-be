using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace medical_be.Models;

public class MedicalFile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // File type relationship
    [Required]
    public int TypeId { get; set; }
    [ForeignKey("TypeId")]
    public virtual FileType Type { get; set; } = null!;

    // File information
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;

    [Required]
    public long Size { get; set; }

    [MaxLength(10)]
    public string? Extension { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    // Polymorphic relationship (what entity this file belongs to)
    [MaxLength(100)]
    public string? ModelType { get; set; } // "User", "MedicalRecord", "VisitRecord", etc.

    public string? ModelId { get; set; } // ID of the related entity

    // User tracking
    public string? CreatedById { get; set; }
    [ForeignKey("CreatedById")]
    public virtual User? CreatedBy { get; set; }

    public string? UpdatedById { get; set; }
    [ForeignKey("UpdatedById")]
    public virtual User? UpdatedBy { get; set; }

    public string? DeletedById { get; set; }
    [ForeignKey("DeletedById")]
    public virtual User? DeletedBy { get; set; }

    // Security
    [MaxLength(500)]
    public string? Password { get; set; } // For encrypted files

    [MaxLength(200)]
    public string? Label { get; set; } // Custom label for the file

    // File status
    public bool IsTemporary { get; set; } = false;

    // Image-specific properties
    [MaxLength(100)]
    public string? BlurHash { get; set; } // For image previews

    public int? Width { get; set; }
    public int? Height { get; set; }

    // Metadata
    [MaxLength(1000)]
    public string? Metadata { get; set; } // JSON string for additional metadata

    // Soft delete
    public DateTime? DeletedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Helper properties
    [NotMapped]
    public bool IsDeleted => DeletedAt.HasValue;

    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(Label) ? Label : Name;

    [NotMapped]
    public string SizeFormatted => FormatBytes(Size);

    [NotMapped]
    public bool IsImage => MimeType?.StartsWith("image/") == true;

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
}
