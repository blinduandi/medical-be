using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using medical_be.Extensions;
using medical_be.Controllers.Base;

namespace medical_be.Controllers
{
    [Route("api/medical-records")]
    [Authorize]
    public class MedicalRecordController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<MedicalRecordController> _logger;
        private readonly IPatientAccessLogService _patientAccessLogService;

        public MedicalRecordController(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<MedicalRecordController> logger,
            IPatientAccessLogService patientAccessLogService)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
            _patientAccessLogService = patientAccessLogService;
        }

        /// <summary>
        /// Get all medical records for a patient
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Doctor,Admin,Patient")]
        public async Task<IActionResult> GetMedicalRecords([FromQuery] string? patientId)
        {
            try
            {
                var userId = User.GetUserId();
                
                // If no patientId provided, return records for current user if they're a patient
                if (string.IsNullOrEmpty(patientId))
                {
                    if (User.IsInRole("Patient"))
                    {
                        patientId = userId;
                    }
                    else
                    {
                        return ValidationErrorResponse("PatientId is required for doctors and admins");
                    }
                }

                // If patient role, can only access their own records
                if (User.IsInRole("Patient") && userId != patientId)
                {
                    return ForbiddenResponse("You can only access your own medical records");
                }

                var medicalRecords = await _context.MedicalRecords
                    .Where(mr => mr.PatientId == patientId)
                    .Include(mr => mr.Doctor)
                    .Include(mr => mr.Patient)
                    .OrderByDescending(mr => mr.RecordDate)
                    .Select(mr => new MedicalRecordDto
                    {
                        Id = mr.Id,
                        PatientId = mr.PatientId,
                        DoctorId = mr.DoctorId,
                        AppointmentId = mr.AppointmentId,
                        Diagnosis = mr.Diagnosis,
                        Symptoms = mr.Symptoms,
                        Treatment = mr.Treatment,
                        Prescription = mr.Prescription,
                        Notes = mr.Notes,
                        RecordDate = mr.RecordDate,
                        CreatedAt = mr.CreatedAt,
                        UpdatedAt = mr.UpdatedAt,
                        PatientName = mr.Patient.FirstName + " " + mr.Patient.LastName,
                        DoctorName = mr.Doctor.FirstName + " " + mr.Doctor.LastName
                    })
                    .ToListAsync();

                // Log access
                await _patientAccessLogService.LogPatientAccessAsync(
                    userId,
                    patientId,
                    "ViewMedicalRecords",
                    $"Viewed {medicalRecords.Count} medical records",
                    Request.GetClientIpAddress()
                );

                return SuccessResponse(medicalRecords, "Medical records retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting medical records");
                return InternalServerErrorResponse("An error occurred while retrieving medical records");
            }
        }

        /// <summary>
        /// Get a specific medical record by ID
        /// </summary>
        [HttpGet("{recordId}")]
        [Authorize(Roles = "Doctor,Admin,Patient")]
        public async Task<IActionResult> GetMedicalRecord(int recordId)
        {
            try
            {
                var userId = User.GetUserId();

                var medicalRecord = await _context.MedicalRecords
                    .Where(mr => mr.Id == recordId)
                    .Include(mr => mr.Doctor)
                    .Include(mr => mr.Patient)
                    .Select(mr => new MedicalRecordDto
                    {
                        Id = mr.Id,
                        PatientId = mr.PatientId,
                        DoctorId = mr.DoctorId,
                        AppointmentId = mr.AppointmentId,
                        Diagnosis = mr.Diagnosis,
                        Symptoms = mr.Symptoms,
                        Treatment = mr.Treatment,
                        Prescription = mr.Prescription,
                        Notes = mr.Notes,
                        RecordDate = mr.RecordDate,
                        CreatedAt = mr.CreatedAt,
                        UpdatedAt = mr.UpdatedAt,
                        PatientName = mr.Patient.FirstName + " " + mr.Patient.LastName,
                        DoctorName = mr.Doctor.FirstName + " " + mr.Doctor.LastName
                    })
                    .FirstOrDefaultAsync();

                if (medicalRecord == null)
                {
                    return NotFoundResponse("Medical record not found");
                }

                // If patient role, can only access their own records
                if (User.IsInRole("Patient") && userId != medicalRecord.PatientId)
                {
                    return ForbiddenResponse("You can only access your own medical records");
                }

                // Log access
                await _patientAccessLogService.LogPatientAccessAsync(
                    userId,
                    medicalRecord.PatientId,
                    "ViewMedicalRecord",
                    $"Viewed medical record {recordId}",
                    Request.GetClientIpAddress()
                );

                return SuccessResponse(medicalRecord, "Medical record retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting medical record {RecordId}", recordId);
                return InternalServerErrorResponse("An error occurred while retrieving the medical record");
            }
        }

        /// <summary>
        /// Create a new medical record
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> CreateMedicalRecord([FromBody] CreateMedicalRecordDto dto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                if (string.IsNullOrEmpty(dto.PatientId))
                {
                    return ValidationErrorResponse("PatientId is required");
                }

                var doctorId = User.GetUserId();

                // Verify patient exists
                var patient = await _context.Users.FindAsync(dto.PatientId);
                if (patient == null)
                {
                    return NotFoundResponse("Patient not found");
                }

                var medicalRecord = new MedicalRecord
                {
                    PatientId = dto.PatientId,
                    DoctorId = doctorId,
                    AppointmentId = dto.AppointmentId,
                    Diagnosis = dto.Diagnosis,
                    Symptoms = dto.Symptoms,
                    Treatment = dto.Treatment,
                    Prescription = dto.Prescription,
                    Notes = dto.Notes,
                    RecordDate = dto.RecordDate ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MedicalRecords.Add(medicalRecord);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    doctorId,
                    "MedicalRecordCreated",
                    $"Created medical record for patient: {dto.PatientId}. Diagnosis: {dto.Diagnosis}",
                    "MedicalRecord",
                    null,
                    Request.GetClientIpAddress()
                );

                // Log access
                await _patientAccessLogService.LogPatientAccessAsync(
                    doctorId,
                    dto.PatientId,
                    "CreateMedicalRecord",
                    $"Created medical record with diagnosis: {dto.Diagnosis}",
                    Request.GetClientIpAddress()
                );

                var result = await _context.MedicalRecords
                    .Where(mr => mr.Id == medicalRecord.Id)
                    .Include(mr => mr.Doctor)
                    .Include(mr => mr.Patient)
                    .Select(mr => new MedicalRecordDto
                    {
                        Id = mr.Id,
                        PatientId = mr.PatientId,
                        DoctorId = mr.DoctorId,
                        AppointmentId = mr.AppointmentId,
                        Diagnosis = mr.Diagnosis,
                        Symptoms = mr.Symptoms,
                        Treatment = mr.Treatment,
                        Prescription = mr.Prescription,
                        Notes = mr.Notes,
                        RecordDate = mr.RecordDate,
                        CreatedAt = mr.CreatedAt,
                        UpdatedAt = mr.UpdatedAt,
                        PatientName = mr.Patient.FirstName + " " + mr.Patient.LastName,
                        DoctorName = mr.Doctor.FirstName + " " + mr.Doctor.LastName
                    })
                    .FirstOrDefaultAsync();

                return CreatedAtAction(nameof(GetMedicalRecord), new { recordId = medicalRecord.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating medical record");
                return InternalServerErrorResponse("An error occurred while creating the medical record");
            }
        }

        /// <summary>
        /// Update a medical record
        /// </summary>
        [HttpPut("{recordId}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> UpdateMedicalRecord(int recordId, [FromBody] UpdateMedicalRecordDto dto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                var medicalRecord = await _context.MedicalRecords
                    .Where(mr => mr.Id == recordId)
                    .FirstOrDefaultAsync();

                if (medicalRecord == null)
                {
                    return NotFoundResponse("Medical record not found");
                }

                // Only the doctor who created it or admin can update
                var userId = User.GetUserId();
                if (!User.IsInRole("Admin") && medicalRecord.DoctorId != userId)
                {
                    return ForbiddenResponse("You can only update medical records you created");
                }

                // Update fields
                medicalRecord.Diagnosis = dto.Diagnosis;
                medicalRecord.Symptoms = dto.Symptoms;
                medicalRecord.Treatment = dto.Treatment;
                medicalRecord.Prescription = dto.Prescription;
                medicalRecord.Notes = dto.Notes;
                if (dto.RecordDate.HasValue)
                {
                    medicalRecord.RecordDate = dto.RecordDate.Value;
                }
                medicalRecord.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    userId,
                    "MedicalRecordUpdated",
                    $"Updated medical record {recordId} for patient: {medicalRecord.PatientId}",
                    "MedicalRecord",
                    null,
                    Request.GetClientIpAddress()
                );

                // Log access
                await _patientAccessLogService.LogPatientAccessAsync(
                    userId,
                    medicalRecord.PatientId,
                    "UpdateMedicalRecord",
                    $"Updated medical record {recordId}",
                    Request.GetClientIpAddress()
                );

                var result = await _context.MedicalRecords
                    .Where(mr => mr.Id == recordId)
                    .Include(mr => mr.Doctor)
                    .Include(mr => mr.Patient)
                    .Select(mr => new MedicalRecordDto
                    {
                        Id = mr.Id,
                        PatientId = mr.PatientId,
                        DoctorId = mr.DoctorId,
                        AppointmentId = mr.AppointmentId,
                        Diagnosis = mr.Diagnosis,
                        Symptoms = mr.Symptoms,
                        Treatment = mr.Treatment,
                        Prescription = mr.Prescription,
                        Notes = mr.Notes,
                        RecordDate = mr.RecordDate,
                        CreatedAt = mr.CreatedAt,
                        UpdatedAt = mr.UpdatedAt,
                        PatientName = mr.Patient.FirstName + " " + mr.Patient.LastName,
                        DoctorName = mr.Doctor.FirstName + " " + mr.Doctor.LastName
                    })
                    .FirstOrDefaultAsync();

                return SuccessResponse(result, "Medical record updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating medical record {RecordId}", recordId);
                return InternalServerErrorResponse("An error occurred while updating the medical record");
            }
        }

        /// <summary>
        /// Delete a medical record
        /// </summary>
        [HttpDelete("{recordId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMedicalRecord(int recordId)
        {
            try
            {
                var medicalRecord = await _context.MedicalRecords
                    .Where(mr => mr.Id == recordId)
                    .FirstOrDefaultAsync();

                if (medicalRecord == null)
                {
                    return NotFoundResponse("Medical record not found");
                }

                var patientId = medicalRecord.PatientId;
                _context.MedicalRecords.Remove(medicalRecord);
                await _context.SaveChangesAsync();

                // Audit log
                var userId = User.GetUserId();
                await _auditService.LogAuditAsync(
                    userId,
                    "MedicalRecordDeleted",
                    $"Deleted medical record {recordId} for patient: {patientId}",
                    "MedicalRecord",
                    null,
                    Request.GetClientIpAddress()
                );

                // Log access
                await _patientAccessLogService.LogPatientAccessAsync(
                    userId,
                    patientId,
                    "DeleteMedicalRecord",
                    $"Deleted medical record {recordId}",
                    Request.GetClientIpAddress()
                );

                return SuccessResponse(null, "Medical record deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting medical record {RecordId}", recordId);
                return InternalServerErrorResponse("An error occurred while deleting the medical record");
            }
        }
    }
}
