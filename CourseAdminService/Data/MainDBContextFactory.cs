using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CourseAdminService.Services;
using System;
using System.Linq;

namespace CourseAdminService.Data
{
    public class MainDbContextFactory : IDbContextFactory<MainDBContext>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TenantContext _tenantContext;
        private readonly ILogger<MainDbContextFactory> _logger;

        public MainDbContextFactory(IServiceProvider serviceProvider, TenantContext tenantContext, ILogger<MainDbContextFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public MainDBContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<MainDBContext>();

            var options = _serviceProvider.GetRequiredService<DbContextOptions<MainDBContext>>();
            var connectionString = options.Extensions.OfType<RelationalOptionsExtension>().First().ConnectionString;

            optionsBuilder.UseNpgsql(connectionString);

            var tenantId = _tenantContext.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogError("Tenant-ID is missing from the request.");
                throw new InvalidOperationException("Tenant-ID is required.");
            }

            _logger.LogInformation($"Creating DbContext for tenant: {tenantId}");

            return new MainDBContext(optionsBuilder.Options, _tenantContext);
        }
    }
}