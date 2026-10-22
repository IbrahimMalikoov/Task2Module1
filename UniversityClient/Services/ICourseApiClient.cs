using UniversityClient.Models;

namespace UniversityClient.Services;

public interface ICourseApiClient
{
    Task<ClientResponse<IEnumerable<CourseViewModel>>> GetCoursesAsync(string? search = null);
    Task<ClientResponse<CourseViewModel>> GetCourseByIdAsync(int id);
}
