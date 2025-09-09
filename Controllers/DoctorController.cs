using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using medical_be.Extensions;
using AutoMapper;

namespace medical_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DoctorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<DoctorController> _logger;

        public DoctorController(
            ApplicationDbContext context,
            IMapper mapper,
            IAuditService auditService,
            ILogger<DoctorController> logger)
        {
            _context = context;
            _mapper = mapper;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Search doctors by IDNP
        /// </summary>
        [HttpPost("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchByIDNP([FromBody] DoctorSearchDto searchDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchDto.IDNP))
                {
                    return BadRequest("IDNP is required");
                }

                var doctor = await _context.Users
                    .Where(u => u.IDNP == searchDto.IDNP)
                    .Select(u => new DoctorProfileDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber ?? string.Empty,
                        IDNP = u.IDNP,
                        ClinicId = u.ClinicId,
                        TotalPatients = 0, 
                        LastActivity = null,
                        DateOfBirth = u.DateOfBirth,
                        Address = u.Address,
                        IsActive = u.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (doctor == null)
                {
                    return NotFound("Doctor not found");
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "DoctorSearch", $"Searched doctor with IDNP: {searchDto.IDNP}", "Doctor", null, Request.GetClientIpAddress());

                return Ok(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patient by IDNP: {IDNP}", searchDto.IDNP);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
