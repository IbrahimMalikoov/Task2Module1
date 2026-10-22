using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityApi.Common;
using UniversityApi.DTO;
using UniversityApi.Models;
using UniversityApi.Repositories;

namespace UniversityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IRepository<Enrollment> _enrollmentRepo;
    private readonly IRepository<Student> _studentRepo;
    private readonly IRepository<Course> _courseRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public EnrollmentsController(
        IRepository<Enrollment> enrollmentRepo,
        IRepository<Student> studentRepo,
        IRepository<Course> courseRepo,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _enrollmentRepo = enrollmentRepo;
        _studentRepo = studentRepo;
        _courseRepo = courseRepo;
        _userManager = userManager;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ReturnResult<IEnumerable<EnrollmentDto>>>> GetAll()
    {
        if (User.IsInRole("Admin"))
        {
            var all = await _enrollmentRepo.GetAllAsync();
            return Ok(ReturnResult<IEnumerable<EnrollmentDto>>.Success(_mapper.Map<IEnumerable<EnrollmentDto>>(all)));
        }

        if (User.IsInRole("Teacher"))
        {
            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue)
            {
                return StatusCode(403, ReturnResult<IEnumerable<EnrollmentDto>>.Fail("FORBIDDEN", "No teacher profile linked to this user", 403));
            }

            var enrollments = await _enrollmentRepo.Query()
                .Include(e => e.Course)
                .Where(e => e.Course != null && e.Course.TeacherId == teacherId.Value)
                .ToListAsync();

            return Ok(ReturnResult<IEnumerable<EnrollmentDto>>.Success(_mapper.Map<IEnumerable<EnrollmentDto>>(enrollments)));
        }

        if (User.IsInRole("Student"))
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue)
            {
                return StatusCode(403, ReturnResult<IEnumerable<EnrollmentDto>>.Fail("FORBIDDEN", "No student profile linked to this user", 403));
            }

            var enrollments = await _enrollmentRepo.Query()
                .Where(e => e.StudentId == studentId.Value)
                .ToListAsync();

            return Ok(ReturnResult<IEnumerable<EnrollmentDto>>.Success(_mapper.Map<IEnumerable<EnrollmentDto>>(enrollments)));
        }

        return StatusCode(403, ReturnResult<IEnumerable<EnrollmentDto>>.Fail("FORBIDDEN", "Access denied", 403));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ReturnResult<EnrollmentDto>>> GetById(int id)
    {
        var enrollment = await _enrollmentRepo.Query()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
        {
            return NotFound(ReturnResult<EnrollmentDto>.Fail("ENROLLMENT_NOT_FOUND", "Enrollment record not found", 404));
        }

        if (User.IsInRole("Admin"))
        {
            return Ok(ReturnResult<EnrollmentDto>.Success(_mapper.Map<EnrollmentDto>(enrollment)));
        }

        if (User.IsInRole("Teacher"))
        {
            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || enrollment.Course?.TeacherId != teacherId.Value)
            {
                return StatusCode(403, ReturnResult<EnrollmentDto>.Fail("FORBIDDEN", "You can only view enrollments for your courses", 403));
            }

            return Ok(ReturnResult<EnrollmentDto>.Success(_mapper.Map<EnrollmentDto>(enrollment)));
        }

        if (User.IsInRole("Student"))
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue || enrollment.StudentId != studentId.Value)
            {
                return StatusCode(403, ReturnResult<EnrollmentDto>.Fail("FORBIDDEN", "Students cannot view other students' enrollments or grades", 403));
            }

            return Ok(ReturnResult<EnrollmentDto>.Success(_mapper.Map<EnrollmentDto>(enrollment)));
        }

        return StatusCode(403, ReturnResult<EnrollmentDto>.Fail("FORBIDDEN", "Access denied", 403));
    }

    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<ReturnResult<IEnumerable<EnrollmentDto>>>> GetByStudentId(int studentId)
    {
        if (User.IsInRole("Student"))
        {
            var currentStudentId = await GetCurrentStudentIdAsync();
            if (!currentStudentId.HasValue || currentStudentId.Value != studentId)
            {
                return StatusCode(403, ReturnResult<IEnumerable<EnrollmentDto>>.Fail("FORBIDDEN", "Students cannot view other students' enrollments or grades", 403));
            }
        }

        if (User.IsInRole("Teacher"))
        {
            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue)
            {
                return StatusCode(403, ReturnResult<IEnumerable<EnrollmentDto>>.Fail("FORBIDDEN", "No teacher profile linked", 403));
            }

            var enrollments = await _enrollmentRepo.Query()
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId && e.Course != null && e.Course.TeacherId == teacherId.Value)
                .ToListAsync();

            return Ok(ReturnResult<IEnumerable<EnrollmentDto>>.Success(_mapper.Map<IEnumerable<EnrollmentDto>>(enrollments)));
        }

        var allForStudent = await _enrollmentRepo.FindAsync(e => e.StudentId == studentId);
        return Ok(ReturnResult<IEnumerable<EnrollmentDto>>.Success(_mapper.Map<IEnumerable<EnrollmentDto>>(allForStudent)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Student")]
    public async Task<ActionResult<ReturnResult<EnrollmentDto>>> Enroll(EnrollmentCreateDto dto)
    {
        if (User.IsInRole("Student"))
        {
            var currentStudentId = await GetCurrentStudentIdAsync();
            if (!currentStudentId.HasValue || currentStudentId.Value != dto.StudentId)
            {
                return StatusCode(403, ReturnResult<EnrollmentDto>.Fail("FORBIDDEN", "Students can only enroll themselves", 403));
            }
        }

        var studentExists = await _studentRepo.ExistsAsync(s => s.Id == dto.StudentId);
        if (!studentExists)
        {
            return NotFound(ReturnResult<EnrollmentDto>.Fail("STUDENT_NOT_FOUND", "Student does not exist", 404));
        }

        var courseExists = await _courseRepo.ExistsAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
        {
            return NotFound(ReturnResult<EnrollmentDto>.Fail("COURSE_NOT_FOUND", "Course does not exist", 404));
        }

        var alreadyEnrolled = await _enrollmentRepo.ExistsAsync(e => e.StudentId == dto.StudentId && e.CourseId == dto.CourseId);
        if (alreadyEnrolled)
        {
            return Conflict(ReturnResult<EnrollmentDto>.Fail("ALREADY_ENROLLED", "Student is already enrolled in this course", 409));
        }

        var enrollment = _mapper.Map<Enrollment>(dto);
        await _enrollmentRepo.AddAsync(enrollment);

        return StatusCode(201, ReturnResult<EnrollmentDto>.Success(_mapper.Map<EnrollmentDto>(enrollment), 201));
    }

    [HttpPut("{id}/grade")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ReturnResult<EnrollmentDto>>> UpdateGrade(int id, EnrollmentUpdateGradeDto dto)
    {
        var enrollment = await _enrollmentRepo.Query()
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
        {
            return NotFound(ReturnResult<EnrollmentDto>.Fail("ENROLLMENT_NOT_FOUND", "Enrollment record not found", 404));
        }

        if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
        {
            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || enrollment.Course?.TeacherId != teacherId.Value)
            {
                return StatusCode(403, ReturnResult<EnrollmentDto>.Fail("FORBIDDEN", "Teacher cannot update grades for another teacher's course", 403));
            }
        }

        enrollment.Grade = dto.Grade;
        await _enrollmentRepo.UpdateAsync(enrollment);

        return Ok(ReturnResult<EnrollmentDto>.Success(_mapper.Map<EnrollmentDto>(enrollment)));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<string>>> Delete(int id)
    {
        var enrollment = await _enrollmentRepo.GetByIdAsync(id);
        if (enrollment == null)
        {
            return NotFound(ReturnResult<string>.Fail("ENROLLMENT_NOT_FOUND", "Enrollment record not found", 404));
        }

        await _enrollmentRepo.DeleteAsync(enrollment);
        return Ok(ReturnResult<string>.Success("Student removed from course successfully"));
    }

    private async Task<int?> GetCurrentTeacherIdAsync()
    {
        var claim = User.FindFirstValue("TeacherId");
        if (int.TryParse(claim, out var id))
        {
            return id;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;

        var user = await _userManager.FindByIdAsync(userId);
        return user?.TeacherId;
    }

    private async Task<int?> GetCurrentStudentIdAsync()
    {
        var claim = User.FindFirstValue("StudentId");
        if (int.TryParse(claim, out var id))
        {
            return id;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;

        var user = await _userManager.FindByIdAsync(userId);
        return user?.StudentId;
    }
}
