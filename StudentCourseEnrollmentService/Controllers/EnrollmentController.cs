using System.Text.Json;
using StudentCourseEnrollmentService.Data;
using StudentCourseEnrollmentService.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace StudentCourseEnrollmentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentController : ControllerBase
    {
        private readonly EnrollmentDbContext _context;
        private readonly ILogger<EnrollmentController> _logger;

        public EnrollmentController(EnrollmentDbContext context, ILogger<EnrollmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("students/{studentId}")]
        public async Task<IActionResult> GetStudentById(int studentId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Enrollments)  
                    .ThenInclude(e => e.Course)  
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                {
                    return NotFound("Student not found");
                }

                _logger.LogInformation("Student Enrollments:");

                var enrollmentsList = new List<object>(); 

                foreach (var enrollment in student.Enrollments)
                {
                    var enrollmentData = new
                    {
                        EnrollmentId = enrollment.Id,
                        CourseId = enrollment.CourseId,
                        Status = enrollment.Status,
                        EnrollmentDate = enrollment.EnrollmentDate
                    };
    
                    enrollmentsList.Add(enrollmentData); 
                }
                var json = JsonSerializer.Serialize(enrollmentsList);
                return Ok(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student");
                return StatusCode(500, "An error occurred while retrieving student");
            }
        }


        [HttpPost("students/{studentId}/courses")]
        public async Task<IActionResult> Enroll([FromBody] EnrollmentRequest enrollmentRequest)
        {
            try
            {
                var student = await _context.Students.Include(s => s.Enrollments)
                    .FirstOrDefaultAsync(s => s.Id == enrollmentRequest.StudentId);
                var course = await _context.Courses.Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.Id == enrollmentRequest.CourseId);

                if (student == null)
                    return BadRequest("Student does not exist");

                if (course == null)
                    return BadRequest("Course does not exist");

                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e =>
                        e.StudentId == enrollmentRequest.StudentId && e.CourseId == enrollmentRequest.CourseId);

                if (existingEnrollment != null)
                    return BadRequest("Student is already enrolled in this course");

                var enrollment = new Enrollment
                {
                    StudentId = enrollmentRequest.StudentId,
                    CourseId = enrollmentRequest.CourseId,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "Active"
                };

                _logger.LogInformation(
                    $"Received enrollment request: StudentId={enrollmentRequest.StudentId}, CourseId={enrollmentRequest.CourseId}");
                _logger.LogInformation("Now I will add to student then to course the enrollment");

                student.Enrollments ??= new List<Enrollment>();
                course.Enrollments ??= new List<Enrollment>();

                student.Enrollments.Add(enrollment);
                course.Enrollments.Add(enrollment);
                _logger.LogInformation(student.FirstName);
                _logger.LogInformation("testing w hek");
                _logger.LogInformation("Student Enrollments:");
                foreach (var enrollmenttt in student.Enrollments)
                {
                    _logger.LogInformation($"EnrollmentId: {enrollmenttt.Id}, CourseId: {enrollmenttt.CourseId}, Status: {enrollmenttt.Status}, EnrollmentDate: {enrollmenttt.EnrollmentDate}");
                }
                _context.Enrollments.Add(enrollment); 
                /*  _context.Entry(student).State = EntityState.Modified; 
                _context.Entry(course).State = EntityState.Modified;
                */
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Enrollment successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student");
                return StatusCode(500, "An error occurred while processing enrollment");
            }
        }


        [HttpGet("students")]
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
    }
}