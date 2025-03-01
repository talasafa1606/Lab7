using System.ComponentModel.DataAnnotations;

namespace Lab7.Entities;

public class Teacher
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;
}