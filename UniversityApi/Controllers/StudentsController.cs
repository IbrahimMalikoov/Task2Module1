using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniversityApi.Common;
using UniversityApi.DTO;
using UniversityApi.Models;
using UniversityApi.Repositories;

namespace UniversityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IRepository<Student> _studentRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        IRepository<Student> studentRepo,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<StudentsController> logger)
    {
        _studentRepo = studentRepo;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<ReturnResult<IEnumerable<StudentDto>>>> GetAll()
    {
        _logger.LogInformation("Getting all students");
        var students = await _studentRepo.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<StudentDto>>(students);
        return Ok(ReturnResult<IEnumerable<StudentDto>>.Success(dtos));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ReturnResult<StudentDto>>> GetById(int id)
    {
        if (User.IsInRole("Student"))
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue || studentId.Value != id)
            {
                return StatusCode(403, ReturnResult<StudentDto>.Fail("FORBIDDEN", "Students can only access their own student profile", 403));
            }
        }

        var student = await _studentRepo.GetByIdAsync(id);
        if (student == null)
        {
            _logger.LogWarning("Student with id {Id} not found", id);
            return NotFound(ReturnResult<StudentDto>.Fail("STUDENT_NOT_FOUND", "Student not found", 404));
        }

        return Ok(ReturnResult<StudentDto>.Success(_mapper.Map<StudentDto>(student)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<StudentDto>>> Create(StudentCreateDto dto)
    {
        var student = _mapper.Map<Student>(dto);
        await _studentRepo.AddAsync(student);
        _logger.LogInformation("Student created with id {Id}", student.Id);
        return StatusCode(201, ReturnResult<StudentDto>.Success(_mapper.Map<StudentDto>(student), 201));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<StudentDto>>> Update(int id, StudentUpdateDto dto)
    {
        var student = await _studentRepo.GetByIdAsync(id);
        if (student == null)
        {
            return NotFound(ReturnResult<StudentDto>.Fail("STUDENT_NOT_FOUND", "Student not found", 404));
        }

        _mapper.Map(dto, student);
        await _studentRepo.UpdateAsync(student);
        _logger.LogInformation("Student updated with id {Id}", id);
        return Ok(ReturnResult<StudentDto>.Success(_mapper.Map<StudentDto>(student)));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<string>>> Delete(int id)
    {
        var student = await _studentRepo.GetByIdAsync(id);
        if (student == null)
        {
            return NotFound(ReturnResult<string>.Fail("STUDENT_NOT_FOUND", "Student not found", 404));
        }

        await _studentRepo.DeleteAsync(student);
        _logger.LogInformation("Student deleted with id {Id}", id);
        return Ok(ReturnResult<string>.Success("Student deleted successfully"));
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
