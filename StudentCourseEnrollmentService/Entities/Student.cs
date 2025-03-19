using System.Text.Json.Serialization;

namespace StudentCourseEnrollmentService.Entities;

public class Student
{
    public int Id { get; set; }
    public string StudentId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    
    
    [JsonIgnore]
    public ICollection<Enrollment> Enrollments { get; set; }
}