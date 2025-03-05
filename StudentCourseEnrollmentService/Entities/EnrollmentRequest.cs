using System.ComponentModel.DataAnnotations;

namespace StudentCourseEnrollmentService.Entities;

public class EnrollmentRequest
{
    [Key]
    public int Id { get; set; }  
    public int StudentId { get; set; }
    public int CourseId { get; set; }
}
