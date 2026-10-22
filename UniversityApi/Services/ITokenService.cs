using UniversityApi.Models;

namespace UniversityApi.Services;

public interface ITokenService
{
    (string Token, DateTime Expiration) GenerateToken(ApplicationUser user, IList<string> roles);
}
