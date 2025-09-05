using medical_be.Shared.DTOs;
using medical_be.Shared.Interfaces;
using medical_be.Models;
using medical_be.Data;
using Microsoft.EntityFrameworkCore;

namespace medical_be.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAuditAsync(string userId, string action, string details, string? entityType = null, Guid? entityId = null, string? ipAddress = null)
    {
        // Parse to Guid if possible, else use empty
        Guid uid = Guid.Empty;
        Guid.TryParse(userId, out uid);

        try
        {
            var user = await _context.Users.FindAsync(userId);
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                UserId = uid,
                UserEmail = user?.Email ?? "Unknown",
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress ?? GetClientIpAddress(),
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
        }
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
        }
    }

    public async Task<List<AuditLogDto>> GetAuditLogsAsync(DateTime from, DateTime to)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Take(1000)
            .ToListAsync();

        return logs.Select(MapToAuditLogDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetUserAuditLogsAsync(Guid userId, DateTime from, DateTime to)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.UserId == userId && a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .ToListAsync();

        return logs.Select(MapToAuditLogDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetEntityAuditLogsAsync(string entityType, Guid entityId)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return logs.Select(MapToAuditLogDto).ToList();
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(ip, out _))
                return ip;
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && System.Net.IPAddress.TryParse(realIp, out _))
            return realIp;

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static AuditLogDto MapToAuditLogDto(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            Action = auditLog.Action,
            UserId = auditLog.UserId,
            UserEmail = auditLog.UserEmail,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            Details = auditLog.Details,
            IpAddress = auditLog.IpAddress,
            CreatedAt = auditLog.CreatedAt
        };
    }
}
