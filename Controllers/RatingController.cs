using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.DTOs;
using medical_be.Models;
using System.Security.Claims;

namespace medical_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RatingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RatingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create a rating
        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] CreateRatingDto dto)
        {
            var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (patientId == null)
                return Unauthorized();

            // Check if patient already rated this doctor
            var alreadyRated = await _context.Ratings
                .AnyAsync(r => r.DoctorId == dto.DoctorId && r.PatientId == patientId);
            if (alreadyRated)
                return BadRequest(new { message = "You can only rate a doctor once." });

            var rating = new Rating
            {
                DoctorId = dto.DoctorId,
                PatientId = patientId,
                RatingNr = dto.RatingNr,
                RatingCommentary = dto.RatingCommentary,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            // Return all fields from the rating table for the created rating
            return Ok(new RatingDto
            {
                RatingId = rating.RatingId,
                DoctorId = rating.DoctorId,
                PatientId = rating.PatientId,
                RatingNr = rating.RatingNr,
                RatingCommentary = rating.RatingCommentary,
                CreatedAt = rating.CreatedAt
            });
        }

        // Get a specific rating by id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<RatingDto>> GetRating(int id)
        {
            var rating = await _context.Ratings.FindAsync(id);
            if (rating == null)
                return NotFound();

            return new RatingDto
            {
                RatingId = rating.RatingId,
                DoctorId = rating.DoctorId,
                PatientId = rating.PatientId,
                RatingNr = rating.RatingNr,
                RatingCommentary = rating.RatingCommentary,
                CreatedAt = rating.CreatedAt
            };
        }

        // Get all ratings for a doctor with average
        [HttpGet("doctor/{doctorId}")]
        [AllowAnonymous]
        public async Task<ActionResult<DoctorRatingsSummaryDto>> GetDoctorRatings(string doctorId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.DoctorId == doctorId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (!ratings.Any())
                return new DoctorRatingsSummaryDto
                {
                    DoctorId = doctorId,
                    AverageRating = 0,
                    RatingsCount = 0,
                    Ratings = new List<RatingDto>()
                };

            var avg = ratings.Average(r => r.RatingNr);

            return new DoctorRatingsSummaryDto
            {
                DoctorId = doctorId,
                AverageRating = avg,
                RatingsCount = ratings.Count,
                Ratings = ratings.Select(r => new RatingDto
                {
                    RatingId = r.RatingId,
                    DoctorId = r.DoctorId,
                    PatientId = r.PatientId,
                    RatingNr = r.RatingNr,
                    RatingCommentary = r.RatingCommentary,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };
        }
    }
}