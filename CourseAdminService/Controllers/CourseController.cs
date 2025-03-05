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
    private readonly MainDBContext _context;
    private readonly RabbitMQPublisher _publisher;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<CourseController> _logger;

    public CourseController(
        MainDBContext context, 
        RabbitMQPublisher publisher,
        TenantContext tenantContext,
        ILogger<CourseController> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var tenantId = _tenantContext.TenantId;
        _logger.LogInformation($"{tenantId}");

        try
        {
            var courses = await _context.Courses
                .Where(c => c.TenantId.ToLower() == tenantId.ToLower())
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
            {
                return BadRequest("TenantId is required in the header.");
            }

            var course = await _context.Courses
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
public async Task<IActionResult> CreateCourse([FromBody] CourseDto courseDto)
{
  
    try
    {

        _logger.LogInformation("Received course DTO: {@courseDto}", courseDto);

        foreach (var property in courseDto.GetType().GetProperties())
        {
            var propertyValue = property.GetValue(courseDto);
            _logger.LogInformation($"{property.Name}: {propertyValue ?? "null"}");
        }

        if (string.IsNullOrEmpty(courseDto.Code) || string.IsNullOrEmpty(courseDto.Name))
        {
            return BadRequest("Course code and name are required");
        }
        var tenantId = _tenantContext.TenantId;
        _logger.LogInformation($"TenantId in CreateCourse: {tenantId}");

        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogError("TenantId is null or empty.");
            return BadRequest("Tenant identification is required");
        }

        var existingCourse = await _context.Courses
            .FirstOrDefaultAsync(c => 
                c.Code == courseDto.Code && 
                c.TenantId == tenantId);

        if (existingCourse != null)
        {
            _logger.LogWarning($"Course with code {courseDto.Code} already exists for this tenant.");
            return BadRequest($"Course with code {courseDto.Code} already exists for this tenant");
        }

        var course = new Course
        {
            Code = courseDto.Code,
            Name = courseDto.Name,
            Description = courseDto.Description,
            Credits = courseDto.Credits,
            TenantId = tenantId
        };

        _logger.LogInformation("Creating new course: {@course}", course);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

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
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseDto courseDto)
    {
        try
        {
            var course = await _context.Courses.FindAsync(id);
            
            if (course == null)
                return NotFound();
            
            course.Code = courseDto.Code;
            course.Name = courseDto.Name;
            course.Description = courseDto.Description;
            course.Credits = courseDto.Credits;
            
            await _context.SaveChangesAsync();
            
            _publisher.PublishCourse(course);
            
            return Ok(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating course {id}");
            return StatusCode(500, "An error occurred while updating the course");
        }
    }
}
