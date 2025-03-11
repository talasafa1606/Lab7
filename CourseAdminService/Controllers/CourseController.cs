using CourseAdminService.Data;
using CourseAdminService.Services;
using Microsoft.AspNetCore.Mvc;
using CourseAdminService.Entities;
using CourseAdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseAdminService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly IDbContextFactory<MainDBContext> _dbContextFactory;
    private readonly RabbitMQPublisher _publisher;
    private readonly ILogger<CourseController> _logger;

    public CourseController(
        IDbContextFactory<MainDBContext> dbContextFactory,
        RabbitMQPublisher publisher,
        ILogger<CourseController> logger)
    {
        _dbContextFactory = dbContextFactory;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses([FromHeader(Name = "X-Tenant-ID")] string tenantId)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
                return BadRequest("TenantId is required in the header.");

            using var context = _dbContextFactory.CreateDbContext();
            var courses = await context.Courses
                .Where(c => c.TenantId == tenantId)
                .ToListAsync();

            return Ok(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses");
            return StatusCode(500, "An error occurred while retrieving courses");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse(int id, [FromHeader(Name = "X-Tenant-ID")] string tenantId)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
                return BadRequest("TenantId is required in the header.");

            using var context = _dbContextFactory.CreateDbContext();
            var course = await context.Courses
                .Where(c => c.Id == id && c.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound($"Course with ID {id} not found for Tenant '{tenantId}'");

            return Ok(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving course {id}");
            return StatusCode(500, "An error occurred while retrieving the course");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CourseDto courseDto, [FromHeader(Name = "X-Tenant-ID")] string tenantId)
    {
        try
        {
            if (string.IsNullOrEmpty(courseDto.Code) || string.IsNullOrEmpty(courseDto.Name))
                return BadRequest("Course code and name are required");

            if (string.IsNullOrEmpty(tenantId))
                return BadRequest("Tenant identification is required");

            using var context = _dbContextFactory.CreateDbContext();
            
            var existingCourse = await context.Courses
                .FirstOrDefaultAsync(c => c.Code == courseDto.Code && c.TenantId == tenantId);

            if (existingCourse != null)
                return BadRequest($"Course with code {courseDto.Code} already exists for this tenant");

            var course = new Course
            {
                Code = courseDto.Code,
                Name = courseDto.Name,
                Description = courseDto.Description,
                Credits = courseDto.Credits,
                TenantId = tenantId
            };

            context.Courses.Add(course);
            await context.SaveChangesAsync();

            _publisher.PublishCourse(course);

            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            return StatusCode(500, "An error occurred while creating the course");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseDto courseDto, [FromHeader(Name = "X-Tenant-ID")] string tenantId)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
                return BadRequest("TenantId is required in the header.");

            using var context = _dbContextFactory.CreateDbContext();
            var course = await context.Courses
                .Where(c => c.Id == id && c.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound($"Course with ID {id} not found for Tenant '{tenantId}'");

            course.Code = courseDto.Code;
            course.Name = courseDto.Name;
            course.Description = courseDto.Description;
            course.Credits = courseDto.Credits;

            await context.SaveChangesAsync();
            _publisher.PublishCourse(course);

            return Ok(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating course {id}");
            return StatusCode(500, "An error occurred while updating the course");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(int id, [FromHeader(Name = "X-Tenant-ID")] string tenantId)
    {
        try
        {
            if (string.IsNullOrEmpty(tenantId))
                return BadRequest("TenantId is required in the header.");

            using var context = _dbContextFactory.CreateDbContext();
            var course = await context.Courses
                .Where(c => c.Id == id && c.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound($"Course with ID {id} not found for Tenant '{tenantId}'");

            context.Courses.Remove(course);
            await context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting course {id}");
            return StatusCode(500, "An error occurred while deleting the course");
        }
    }
}
