using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ServiceUsers.Data;
using ServiceUsers.Data.Entities;
using ServiceUsers.Infrastructure;
using StudentPass.Contracts;

namespace ServiceUsers.Services;

public sealed class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public string? TryGetEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(ClaimTypes.Email);
    }

    public async Task<User> GetRequiredUserAsync(CancellationToken cancellationToken)
    {
        var email = TryGetEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new HttpDetailException(401, "Не предоставлен токен");
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new HttpDetailException(401, "Пользователь не найден или удалён");
        }

        return user;
    }

    public async Task RequireAdminAsync(CancellationToken cancellationToken)
    {
        var user = await GetRequiredUserAsync(cancellationToken);
        if (user.Role != UserRole.Admin)
        {
            throw new HttpDetailException(403, "Доступ только для администраторов");
        }
    }

    public async Task<Partner> RequirePartnerAsync(PartnerService partners, CancellationToken cancellationToken)
    {
        var user = await GetRequiredUserAsync(cancellationToken);
        if (user.Role != UserRole.Partner)
        {
            throw new HttpDetailException(403, "Доступ только для партнёров");
        }

        return await partners.RequireApprovedPartnerAsync(user.Email, cancellationToken);
    }
}
