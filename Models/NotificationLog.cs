using System.ComponentModel.DataAnnotations;

namespace medical_be.Models;

public class NotificationLog
{
    public Guid Id { get; set; }
    
    public string? UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string RecipientEmail { get; set; } = string.Empty;
    
    [MaxLength(15)]
    public string? RecipientPhone { get; set; }
    
    [Required]
    public NotificationType Type { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual User? User { get; set; }
}

public enum NotificationType
{
    Email = 1,
    SMS = 2,
    Push = 3
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}
