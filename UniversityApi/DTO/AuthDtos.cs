using System.ComponentModel.DataAnnotations;

namespace UniversityApi.DTO;

public class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public int? TeacherId { get; set; }

    public int? StudentId { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string Username { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

public class CurrentUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
}

public class AssignRoleDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
