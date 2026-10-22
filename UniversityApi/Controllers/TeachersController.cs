using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityApi.Common;
using UniversityApi.DTO;
using UniversityApi.Models;
using UniversityApi.Repositories;

namespace UniversityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly IRepository<Teacher> _teacherRepo;
    private readonly IMapper _mapper;

    public TeachersController(IRepository<Teacher> teacherRepo, IMapper mapper)
    {
        _teacherRepo = teacherRepo;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ReturnResult<IEnumerable<TeacherDto>>>> GetAll()
    {
        var teachers = await _teacherRepo.GetAllAsync();
        return Ok(ReturnResult<IEnumerable<TeacherDto>>.Success(_mapper.Map<IEnumerable<TeacherDto>>(teachers)));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ReturnResult<TeacherDto>>> GetById(int id)
    {
        var teacher = await _teacherRepo.GetByIdAsync(id);
        if (teacher == null)
        {
            return NotFound(ReturnResult<TeacherDto>.Fail("TEACHER_NOT_FOUND", "Teacher not found", 404));
        }

        return Ok(ReturnResult<TeacherDto>.Success(_mapper.Map<TeacherDto>(teacher)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<TeacherDto>>> Create(TeacherCreateDto dto)
    {
        var teacher = _mapper.Map<Teacher>(dto);
        await _teacherRepo.AddAsync(teacher);
        return StatusCode(201, ReturnResult<TeacherDto>.Success(_mapper.Map<TeacherDto>(teacher), 201));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<string>>> Delete(int id)
    {
        var teacher = await _teacherRepo.GetByIdAsync(id);
        if (teacher == null)
        {
            return NotFound(ReturnResult<string>.Fail("TEACHER_NOT_FOUND", "Teacher not found", 404));
        }

        await _teacherRepo.DeleteAsync(teacher);
        return Ok(ReturnResult<string>.Success("Teacher deleted successfully"));
    }
}
