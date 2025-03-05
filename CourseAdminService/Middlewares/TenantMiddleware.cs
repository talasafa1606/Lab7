using CourseAdminService.Services;

namespace CourseAdminService.Middlewares;
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(
        RequestDelegate next, 
        ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        try 
        {
            // Extract TenantId from various sources
            var tenantId = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();

            if (string.IsNullOrEmpty(tenantId))
            {
                var host = context.Request.Host.Host;
                var subdomains = host.Split('.');
                tenantId = subdomains.Length > 1 ? subdomains[0] : null;
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = context.Request.Query["tenantId"].FirstOrDefault();
            }

            _logger.LogInformation($"TenantId extracted: {tenantId}");

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("No tenant identifier found");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    Message = "Tenant identification is required",
                    Methods = new[] 
                    {
                        "Use X-Tenant-ID header",
                        "Use subdomain",
                        "Use tenantId query parameter"
                    }
                });
                return;
            }

            // Validate tenant against known tenants (optional)
            var validTenants = new[] { "university_a", "university_b" };
            if (!validTenants.Contains(tenantId))
            {
                _logger.LogWarning($"Invalid tenant: {tenantId}");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    Message = "Invalid tenant",
                    ValidTenants = validTenants
                });
                return;
            }

            // Log tenant information
            _logger.LogInformation($"Processing request for tenant: {tenantId}");

            // Set tenant ID in the TenantContext
            tenantContext.TenantId = tenantId;
            _logger.LogInformation($"aam jarrib: {tenantContext.TenantId}");

            // Continue processing the request
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant middleware");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new 
            { 
                Message = "An error occurred processing the tenant context" 
            });
        }
    }
}
