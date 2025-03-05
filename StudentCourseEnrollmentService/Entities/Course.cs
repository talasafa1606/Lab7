namespace StudentCourseEnrollmentService.Entities;

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Credits { get; set; }
    
    public ICollection<Enrollment> Enrollments { get; set; }
}
