using System.Security.Claims;
using DigitalWallet.Application.Interfaces.Services;

namespace DigitalWallet.API.Identities;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            string? userId =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;

            if (!int.TryParse(userId, out int parsedUserId))
            {
                throw new InvalidOperationException("The authenticated user has no valid user ID.");
            }

            return parsedUserId;
        }
    }
}
