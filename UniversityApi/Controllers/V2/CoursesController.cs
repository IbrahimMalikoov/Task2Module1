using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityApi.Common;
using UniversityApi.DTO;
using UniversityApi.Services;

namespace UniversityApi.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/courses")]
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
    public async Task<ActionResult<ReturnResult<IEnumerable<CourseV2Dto>>>> GetAll([FromQuery] string? search)
    {
        var courses = await _courseService.GetAllAsync(search);
        var dtos = _mapper.Map<IEnumerable<CourseV2Dto>>(courses);
        return Ok(ReturnResult<IEnumerable<CourseV2Dto>>.Success(dtos));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReturnResult<CourseV2Dto>>> GetById(int id)
    {
        var course = await _courseService.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound(ReturnResult<CourseV2Dto>.Fail("COURSE_NOT_FOUND", "Course not found", 404));
        }

        return Ok(ReturnResult<CourseV2Dto>.Success(_mapper.Map<CourseV2Dto>(course)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<CourseV2Dto>>> Create(CourseCreateDto dto)
    {
        var (success, errorCode, errorMessage, course) = await _courseService.CreateAsync(dto);
        if (!success)
        {
            return BadRequest(ReturnResult<CourseV2Dto>.Fail(errorCode ?? "CREATE_FAILED", errorMessage ?? "Failed to create course", 400));
        }

        var courseDto = _mapper.Map<CourseV2Dto>(course);
        return StatusCode(201, ReturnResult<CourseV2Dto>.Success(courseDto, 201));
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
