using CourseAdminService.Services;
using CourseAdminService.Data;
using CourseAdminService.Middlewares;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<TenantContext>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/* Uncomment and modify if needed
builder.Services.AddDbContext<MainDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
*/
builder.Services.AddHttpContextAccessor();


// Configure DbContext with multi-tenancy
builder.Services.AddDbContext<MainDBContext>((serviceProvider, options) =>
{
    var tenantContext = serviceProvider.GetRequiredService<TenantContext>();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Configure multi-tenancy by setting the schema dynamically
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", tenantContext.GetSchemaName());
    });
});

builder.Services.AddSingleton<RabbitMQPublisher>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MainDBContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<TenantMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();