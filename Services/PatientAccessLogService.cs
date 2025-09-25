using AutoMapper;
using medical_be.Data;
using medical_be.DTOs;
using medical_be.Models;
using medical_be.Extensions;
using Microsoft.EntityFrameworkCore;

namespace medical_be.Services;

public interface IPatientAccessLogService
{
    Task LogPatientAccessAsync(string doctorId, string patientId, string accessType, string? accessReason = null, string? ipAddress = null, string? userAgent = null);
    Task<List<PatientAccessLogDto>> GetPatientAccessLogsAsync(PatientAccessLogQueryDto query);
    Task<PatientAccessLogSummaryDto> GetPatientAccessSummaryAsync(string patientId);
    Task<List<PatientAccessLogDto>> GetDoctorAccessHistoryAsync(string doctorId, int page = 1, int pageSize = 20);
}

public class PatientAccessLogService : IPatientAccessLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientAccessLogService> _logger;

    public PatientAccessLogService(ApplicationDbContext context, IMapper mapper, ILogger<PatientAccessLogService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task LogPatientAccessAsync(string doctorId, string patientId, string accessType, string? accessReason = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var accessLog = new PatientAccessLog
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                PatientId = patientId,
                AccessType = accessType,
                AccessReason = accessReason,
                AccessedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.PatientAccessLogs.Add(accessLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Patient access logged: Doctor {DoctorId} accessed Patient {PatientId} for {AccessType}", 
                doctorId, patientId, accessType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging patient access: Doctor {DoctorId}, Patient {PatientId}, AccessType {AccessType}", 
                doctorId, patientId, accessType);
            // Don't throw exception to avoid disrupting the main operation
        }
    }

    public async Task<List<PatientAccessLogDto>> GetPatientAccessLogsAsync(PatientAccessLogQueryDto query)
    {
        try
        {
            var queryable = _context.PatientAccessLogs
                .Include(pal => pal.Patient)
                .Include(pal => pal.Doctor)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.PatientId))
                queryable = queryable.Where(pal => pal.PatientId == query.PatientId);

            if (!string.IsNullOrEmpty(query.DoctorId))
                queryable = queryable.Where(pal => pal.DoctorId == query.DoctorId);

            if (query.FromDate.HasValue)
                queryable = queryable.Where(pal => pal.AccessedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                queryable = queryable.Where(pal => pal.AccessedAt <= query.ToDate.Value);

            if (!string.IsNullOrEmpty(query.AccessType))
                queryable = queryable.Where(pal => pal.AccessType == query.AccessType);

            var results = await queryable
                .OrderByDescending(pal => pal.AccessedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return _mapper.Map<List<PatientAccessLogDto>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient access logs");
            return new List<PatientAccessLogDto>();
        }
    }

    public async Task<PatientAccessLogSummaryDto> GetPatientAccessSummaryAsync(string patientId)
    {
        try
        {
            var logs = await _context.PatientAccessLogs
                .Include(pal => pal.Doctor)
                .Include(pal => pal.Patient)
                .Where(pal => pal.PatientId == patientId)
                .OrderByDescending(pal => pal.AccessedAt)
                .ToListAsync();

            var summary = new PatientAccessLogSummaryDto
            {
                TotalAccesses = logs.Count,
                UniqueDoctors = logs.Select(l => l.DoctorId).Distinct().Count(),
                LastAccess = logs.FirstOrDefault()?.AccessedAt,
                LastAccessedBy = logs.FirstOrDefault()?.Doctor?.FirstName + " " + logs.FirstOrDefault()?.Doctor?.LastName,
                AccessTypeCount = logs.GroupBy(l => l.AccessType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecentAccesses = _mapper.Map<List<PatientAccessLogDto>>(logs.Take(10).ToList())
            };

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient access summary for patient {PatientId}", patientId);
            return new PatientAccessLogSummaryDto();
        }
    }

    public async Task<List<PatientAccessLogDto>> GetDoctorAccessHistoryAsync(string doctorId, int page = 1, int pageSize = 20)
    {
        try
        {
            var logs = await _context.PatientAccessLogs
                .Include(pal => pal.Patient)
                .Include(pal => pal.Doctor)
                .Where(pal => pal.DoctorId == doctorId)
                .OrderByDescending(pal => pal.AccessedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return _mapper.Map<List<PatientAccessLogDto>>(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor access history for doctor {DoctorId}", doctorId);
            return new List<PatientAccessLogDto>();
        }
    }
}