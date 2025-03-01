using System.ComponentModel.DataAnnotations;

namespace Lab7.Entities;

public class Student
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    public int Age { get; set; }
}

