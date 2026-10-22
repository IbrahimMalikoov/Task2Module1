namespace UniversityClient.Models;

public class CourseViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int TeacherId { get; set; }
    public TeacherViewModel? Teacher { get; set; }
    public int EnrolledStudentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TeacherViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class ApiResult<T>
{
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public T? Result { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string TraceId { get; set; } = string.Empty;
}

public class ClientResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int? StatusCode { get; set; }
}
