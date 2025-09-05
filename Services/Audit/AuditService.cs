using medical_be.Shared.DTOs;
using medical_be.Shared.Interfaces;
using medical_be.Models;
using medical_be.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace medical_be.Services.Audit;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogActivityAsync(string action, Guid userId, string details, string? entityType = null, Guid? entityId = null)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId.ToString());
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                UserId = userId,
                UserEmail = user?.Email ?? "Unknown",
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = GetClientIpAddress(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {Action} by {UserEmail}", action, auditLog.UserEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for action: {Action}", action);
            // Don't throw here - audit logging shouldn't break the main flow
        }
    }

    public async Task<List<AuditLogDto>> GetAuditLogsAsync(DateTime from, DateTime to)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Take(1000) // Limit results for performance
            .ToListAsync();

        return _mapper.Map<List<AuditLogDto>>(logs);
    }

    public async Task<List<AuditLogDto>> GetUserAuditLogsAsync(Guid userId, DateTime from, DateTime to)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.UserId == userId && a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Take(500) // Limit results for performance
            .ToListAsync();

        return _mapper.Map<List<AuditLogDto>>(logs);
    }

    public async Task<List<AuditLogDto>> GetEntityAuditLogsAsync(string entityType, Guid entityId)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<AuditLogDto>>(logs);
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Check for forwarded IP first (when behind proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(ip, out _))
                return ip;
        }

        // Check for real IP (another proxy header)
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && System.Net.IPAddress.TryParse(realIp, out _))
            return realIp;

        // Fall back to connection remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
