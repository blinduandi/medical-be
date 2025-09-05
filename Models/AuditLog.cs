using System.ComponentModel.DataAnnotations;

namespace medical_be.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string UserEmail { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? EntityType { get; set; }
    
    public Guid? EntityId { get; set; }
    
    [MaxLength(2000)]
    public string? Details { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
