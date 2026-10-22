using Microsoft.AspNetCore.Mvc;
using UniversityClient.Models;
using UniversityClient.Services;

namespace UniversityClient.Controllers;

public class CoursesController : Controller
{
    private readonly ICourseApiClient _courseClient;

    public CoursesController(ICourseApiClient courseClient)
    {
        _courseClient = courseClient;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var response = await _courseClient.GetCoursesAsync(search);
        return View(response);
    }

    public async Task<IActionResult> Details(int id)
    {
        var response = await _courseClient.GetCourseByIdAsync(id);
        return View(response);
    }
}
