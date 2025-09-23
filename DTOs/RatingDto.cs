using System.ComponentModel.DataAnnotations;


namespace medical_be.DTOs
{
    public class CreateRatingDto
    {
        [Required]
        public string PatientId { get; set; } = string.Empty;

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int RatingNr { get; set; }

        [MaxLength(1000)]
        public string? RatingCommentary { get; set; }
    }

    public class RatingDto
    {
        public int RatingId { get; set; }
        public string DoctorId { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public int RatingNr { get; set; }
        public string? RatingCommentary { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DoctorRatingsSummaryDto
    {
        public string DoctorId { get; set; } = null!;
        public double AverageRating { get; set; }
        public int RatingsCount { get; set; }
        public List<RatingDto> Ratings { get; set; } = new();
    }
}