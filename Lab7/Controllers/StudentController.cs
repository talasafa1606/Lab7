using Lab7;
using Lab7.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lab7.Services;
using Lab7.Entities;

[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly KeycloakUserService _keycloakService;
    private readonly BlobStorageService _storageService;
    private readonly RabbitMQPublisher _publisher;
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        ApplicationDbContext context,
        KeycloakUserService keycloakService,
        BlobStorageService storageService,
        RabbitMQPublisher publisher,
        ILogger<StudentController> logger)
    {
        _context = context;
        _keycloakService = keycloakService;
        _storageService = storageService;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "TeacherOnly")]
    public async Task<IActionResult> GetStudents()
    {
        try
        {
            var students = await _context.Students.ToListAsync();
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving students");
            return StatusCode(500, "An error occurred while retrieving students");
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetStudent(int id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);
            
            if (student == null)
                return NotFound();
                
            return Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving student {id}");
            return StatusCode(500, "An error occurred while retrieving the student");
        }
    }

    
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AddStudent([FromBody] Student student)
    {
        try
        {
            if (student == null || 
                string.IsNullOrWhiteSpace(student.StudentId) || 
                string.IsNullOrWhiteSpace(student.FirstName) || 
                string.IsNullOrWhiteSpace(student.LastName) || 
                string.IsNullOrWhiteSpace(student.Email))
            {
                return BadRequest("Invalid student data. All fields are required.");
            }

            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == student.StudentId || s.Email == student.Email);

            if (existingStudent != null)
                return Conflict("A student with this StudentId or Email already exists.");

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"New student added: {student.FirstName} {student.LastName} (ID: {student.Id})");

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding student");
            return StatusCode(500, "An error occurred while adding the student");
        }
    }
}
