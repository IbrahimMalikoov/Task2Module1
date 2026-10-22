using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniversityApi.Common;
using UniversityApi.DTO;
using UniversityApi.Models;
using UniversityApi.Repositories;
using UniversityApi.Services;

namespace UniversityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IRepository<Teacher> _teacherRepo;
    private readonly IRepository<Student> _studentRepo;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IRepository<Teacher> teacherRepo,
        IRepository<Student> studentRepo)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _teacherRepo = teacherRepo;
        _studentRepo = studentRepo;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ReturnResult<AuthResponseDto>>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.Username);
        if (user == null)
        {
            return Unauthorized(ReturnResult<AuthResponseDto>.Fail("INVALID_CREDENTIALS", "Invalid username or password", 401));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return StatusCode(403, ReturnResult<AuthResponseDto>.Fail("ACCOUNT_LOCKED", "Account is locked due to multiple failed login attempts", 403));
        }

        if (!result.Succeeded)
        {
            return Unauthorized(ReturnResult<AuthResponseDto>.Fail("INVALID_CREDENTIALS", "Invalid username or password", 401));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiration) = _tokenService.GenerateToken(user, roles);

        var response = new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            Username = user.UserName ?? string.Empty,
            Roles = roles
        };

        return Ok(ReturnResult<AuthResponseDto>.Success(response));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ReturnResult<CurrentUserDto>>> Register(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByNameAsync(dto.Username);
        if (existingUser != null)
        {
            return BadRequest(ReturnResult<CurrentUserDto>.Fail("DUPLICATE_USERNAME", "Username is already taken", 400));
        }

        if (dto.TeacherId.HasValue)
        {
            var teacherExists = await _teacherRepo.ExistsAsync(t => t.Id == dto.TeacherId.Value);
            if (!teacherExists)
            {
                return NotFound(ReturnResult<CurrentUserDto>.Fail("TEACHER_NOT_FOUND", "Referenced teacher does not exist", 404));
            }
        }

        if (dto.StudentId.HasValue)
        {
            var studentExists = await _studentRepo.ExistsAsync(s => s.Id == dto.StudentId.Value);
            if (!studentExists)
            {
                return NotFound(ReturnResult<CurrentUserDto>.Fail("STUDENT_NOT_FOUND", "Referenced student does not exist", 404));
            }
        }

        var user = new ApplicationUser
        {
            UserName = dto.Username,
            Email = dto.Email,
            TeacherId = dto.TeacherId,
            StudentId = dto.StudentId
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return BadRequest(ReturnResult<CurrentUserDto>.Fail("REGISTRATION_FAILED", errors, 400));
        }

        await _userManager.AddToRoleAsync(user, "Student");

        var roles = await _userManager.GetRolesAsync(user);
        var response = new CurrentUserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles,
            TeacherId = user.TeacherId,
            StudentId = user.StudentId
        };

        return StatusCode(201, ReturnResult<CurrentUserDto>.Success(response, 201));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ReturnResult<CurrentUserDto>>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ReturnResult<CurrentUserDto>.Fail("UNAUTHORIZED", "User is not authenticated", 401));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ReturnResult<CurrentUserDto>.Fail("USER_NOT_FOUND", "User not found", 404));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = new CurrentUserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles,
            TeacherId = user.TeacherId,
            StudentId = user.StudentId
        };

        return Ok(ReturnResult<CurrentUserDto>.Success(response));
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnResult<CurrentUserDto>>> AssignRole(AssignRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId) ?? await _userManager.FindByNameAsync(dto.UserId);
        if (user == null)
        {
            return NotFound(ReturnResult<CurrentUserDto>.Fail("USER_NOT_FOUND", "User not found", 404));
        }

        var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
        if (!roleExists)
        {
            return BadRequest(ReturnResult<CurrentUserDto>.Fail("ROLE_NOT_FOUND", $"Role '{dto.Role}' does not exist", 400));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, dto.Role);

        var updatedRoles = await _userManager.GetRolesAsync(user);
        var response = new CurrentUserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = updatedRoles,
            TeacherId = user.TeacherId,
            StudentId = user.StudentId
        };

        return Ok(ReturnResult<CurrentUserDto>.Success(response));
    }
}
