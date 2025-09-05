using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace medical_be.Extensions;

public static class HttpContextExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    public static string? GetClientIpAddress(this HttpRequest request)
    {
        // Check for forwarded IP first (when behind proxy/load balancer)
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(ip, out _))
                return ip;
        }

        // Check for real IP (another proxy header)
        var realIp = request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && System.Net.IPAddress.TryParse(realIp, out _))
            return realIp;

        // Fall back to connection remote IP
        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
