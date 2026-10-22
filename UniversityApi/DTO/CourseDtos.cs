using System.ComponentModel.DataAnnotations;

namespace UniversityApi.DTO;

public class CourseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TeacherId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourseV2Dto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TeacherId { get; set; }
    public TeacherDto? Teacher { get; set; }
    public int EnrolledStudentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourseCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Credits { get; set; }

    [Required]
    public int TeacherId { get; set; }
}
