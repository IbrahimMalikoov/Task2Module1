using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UniversityApi.DTO;
using UniversityApi.Models;
using UniversityApi.Repositories;

namespace UniversityApi.Services;

public class CourseService : ICourseService
{
    private readonly IRepository<Course> _courseRepo;
    private readonly IRepository<Teacher> _teacherRepo;
    private readonly IMapper _mapper;

    public CourseService(IRepository<Course> courseRepo, IRepository<Teacher> teacherRepo, IMapper mapper)
    {
        _courseRepo = courseRepo;
        _teacherRepo = teacherRepo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Course>> GetAllAsync(string? search = null)
    {
        var query = _courseRepo.Query()
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower));
        }

        return await query.ToListAsync();
    }

    public async Task<Course?> GetByIdAsync(int id)
    {
        return await _courseRepo.Query()
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, Course? Course)> CreateAsync(CourseCreateDto dto)
    {
        var teacherExists = await _teacherRepo.ExistsAsync(t => t.Id == dto.TeacherId);
        if (!teacherExists)
        {
            return (false, "TEACHER_NOT_FOUND", "Referenced teacher does not exist", null);
        }

        var course = _mapper.Map<Course>(dto);
        await _courseRepo.AddAsync(course);

        var createdCourse = await GetByIdAsync(course.Id);
        return (true, null, null, createdCourse ?? course);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeleteAsync(int id)
    {
        var course = await _courseRepo.GetByIdAsync(id);
        if (course == null)
        {
            return (false, "COURSE_NOT_FOUND", "Course not found");
        }

        await _courseRepo.DeleteAsync(course);
        return (true, null, null);
    }
}
