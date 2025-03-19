using Microsoft.EntityFrameworkCore;
using StudentCourseEnrollmentService.Data;
using StudentCourseEnrollmentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EnrollmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EnrollmentConnection")));

builder.Services.AddHostedService<RabbitMQConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
//changing to test the push to github eza bymshe my workflow
//another