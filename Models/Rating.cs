using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace medical_be.Models
{
    public class Rating
    {
        [Key]
        public int RatingId { get; set; }

        [Required]
        public string PatientId { get; set; } = string.Empty;

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int RatingNr { get; set; }

        [MaxLength(1000)]
        public string? RatingCommentary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual User Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual User Doctor { get; set; } = null!;
    }
}