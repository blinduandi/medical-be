using System.ComponentModel.DataAnnotations;

namespace medical_be.Models;

public class Permission
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = string.Empty; // e.g., "Users", "Appointments", "Medical Records"

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // e.g., "Create", "Read", "Update", "Delete"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RoleId { get; set; } = string.Empty;

    [Required]
    public int PermissionId { get; set; }

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string? GrantedBy { get; set; }

    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
