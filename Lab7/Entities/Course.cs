using System.ComponentModel.DataAnnotations;

namespace Lab7.Entities;
public class Course
{
    [Key]
    public int CourseId { get; set; }
    [Required]
    public string Title { get; set; }
    [Required]
    public string Description { get; set; }
}