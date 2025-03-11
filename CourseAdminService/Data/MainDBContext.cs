using CourseAdminService.Entities;
using CourseAdminService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourseAdminService.Data
{
    public class MainDBContext : DbContext
    {
        private readonly TenantContext _tenantContext;
        private readonly ILogger<MainDBContext> _logger;

        public MainDBContext(DbContextOptions<MainDBContext> options, TenantContext tenantContext)
            : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var schema = _tenantContext.GetSchemaName();
            _logger.LogInformation($"Applying schema: {schema}");

            modelBuilder.HasDefaultSchema(schema);

            modelBuilder.Entity<Course>().ToTable("Courses", schema);
        }
    }
}