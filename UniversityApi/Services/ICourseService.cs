using UniversityApi.DTO;
using UniversityApi.Models;

namespace UniversityApi.Services;

public interface ICourseService
{
    Task<IEnumerable<Course>> GetAllAsync(string? search = null);
    Task<Course?> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage, Course? Course)> CreateAsync(CourseCreateDto dto);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeleteAsync(int id);
}
