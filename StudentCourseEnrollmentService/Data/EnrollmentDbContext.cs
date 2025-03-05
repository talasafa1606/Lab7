
using StudentCourseEnrollmentService.Entities;
using Microsoft.EntityFrameworkCore;

namespace StudentCourseEnrollmentService.Data;

public class EnrollmentDbContext: DbContext
{
    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options)
        : base(options)
    {
    }
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }

    public DbSet<EnrollmentRequest> EnrollmentRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
