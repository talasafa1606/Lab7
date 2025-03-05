namespace CourseAdminService.Services;

public class TenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantContext> _logger;

    // Property to store the tenant ID in the context
    public string TenantId { get; set; }

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Retrieve the tenant ID from the request headers or other sources (subdomain, query parameter, etc.)
    public string GetTenantId()
    {
        // Tenant ID logic (from headers or other sources)
        if (!string.IsNullOrEmpty(TenantId))
            return TenantId;

        _logger.LogInformation($"In tenantContext i extracted and am sending: {TenantId}");

        // Default or fallback tenant ID logic
        return _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-ID"].FirstOrDefault();
    }

    // Get schema name based on TenantId
    public string GetSchemaName() 
    {
        return $"tenant_{TenantId?.ToLowerInvariant() ?? "default"}";
    }
}