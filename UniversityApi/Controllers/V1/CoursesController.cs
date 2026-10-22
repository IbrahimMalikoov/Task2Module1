using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityApi.Common;
using UniversityApi.DTO;
using UniversityApi.Services;

namespace UniversityApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IMapper _mapper;

    public CoursesController(ICourseService courseService, IMapper mapper)
    {
        _courseService = courseService;
        _mapper = mapper;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ReturnResult<IEnumerable<CourseDto>>>> GetAll([FromQuery] string? search)
    {
        var courses = await _courseService.GetAllAsync(search);
        var dtos = _mapper.Map<IEnumerable<CourseDto>>(courses);
        return Ok(ReturnResult<IEnumerable<CourseDto>>.Success(dtos));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReturnResult<CourseDto>>> GetById(int id)
    {
        var course = await _courseService.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound(ReturnResult<CourseDto>.Fail("COURSE_NOT_FOUND", "Course not found", 404));
        }

        return Ok(ReturnResult<CourseDto>.Success(_mapper.Map<CourseDto>(course)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<CourseDto>>> Create(CourseCreateDto dto)
    {
        var (success, errorCode, errorMessage, course) = await _courseService.CreateAsync(dto);
        if (!success)
        {
            return BadRequest(ReturnResult<CourseDto>.Fail(errorCode ?? "CREATE_FAILED", errorMessage ?? "Failed to create course", 400));
        }

        var courseDto = _mapper.Map<CourseDto>(course);
        return StatusCode(201, ReturnResult<CourseDto>.Success(courseDto, 201));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<string>>> Delete(int id)
    {
        var (success, errorCode, errorMessage) = await _courseService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(ReturnResult<string>.Fail(errorCode ?? "DELETE_FAILED", errorMessage ?? "Failed to delete course", 404));
        }

        return Ok(ReturnResult<string>.Success("Course deleted successfully"));
    }
}
