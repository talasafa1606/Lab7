namespace CourseAdminService.Services;

public class TenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantContext> _logger;

    public string TenantId { get; set; }

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetTenantId()
    {
        if (!string.IsNullOrEmpty(TenantId))
            return TenantId;

        _logger.LogInformation($"In tenantContext i extracted and am sending: {TenantId}");

        return _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-ID"].FirstOrDefault();
    }

    public string GetSchemaName() 
    {
        return $"tenant_{TenantId?.ToLowerInvariant() ?? "default"}";
    }
}