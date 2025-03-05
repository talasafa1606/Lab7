using System.Linq.Expressions;
using CourseAdminService.Entities;
using CourseAdminService.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseAdminService.Data
{
    public class MainDBContext : DbContext
    {
        private readonly TenantContext _tenantContext;

        public MainDBContext(
            DbContextOptions<MainDBContext> options,
            TenantContext tenantContext)
            : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(_tenantContext.GetSchemaName());
            //modelBuilder.Entity<Course>()
              //  .Property(c => c.TenantId)
                //.IsRequired(); 
            
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(Course).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var body = Expression.Equal(
                        Expression.Property(parameter, "TenantId"),
                        Expression.Constant(_tenantContext.TenantId)
                    );
                    var lambda = Expression.Lambda(body, parameter);

                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(lambda);
                }
            }
        }
    }
}