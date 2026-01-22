namespace medical_be.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    // Strict CSP for API - no inline scripts/styles needed
    private const string CspPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "media-src 'self'; " +
        "object-src 'none'; " +
        "frame-src 'none'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "upgrade-insecure-requests";

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use OnStarting to ensure headers are set before response is sent
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Content Security Policy - prevents XSS and data injection attacks
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers["Content-Security-Policy"] = CspPolicy;
            }

            // Prevent MIME type sniffing
            if (!headers.ContainsKey("X-Content-Type-Options"))
            {
                headers["X-Content-Type-Options"] = "nosniff";
            }

            // Prevent clickjacking
            if (!headers.ContainsKey("X-Frame-Options"))
            {
                headers["X-Frame-Options"] = "DENY";
            }

            // Control referrer information
            if (!headers.ContainsKey("Referrer-Policy"))
            {
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            }

            // Restrict browser features
            if (!headers.ContainsKey("Permissions-Policy"))
            {
                headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
            }

            // Prevent caching of sensitive data
            if (!headers.ContainsKey("Cache-Control"))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
            }

            // Remove server disclosure headers
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
