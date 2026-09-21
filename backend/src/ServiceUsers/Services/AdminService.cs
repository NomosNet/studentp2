using Microsoft.EntityFrameworkCore;
using ServiceUsers.Data;
using ServiceUsers.Data.Entities;
using ServiceUsers.Infrastructure;
using StudentPass.Contracts;

namespace ServiceUsers.Services;

public sealed class AdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminPartnerRequestListResponse> GetPartnerRequestsAsync(
        PartnerRequestStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        limit = Math.Clamp(limit, 1, 100);
        var query = _db.PartnerRequests.AsQueryable();
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(req => new PartnerRequestResponse
            {
                Id = req.Id,
                UserEmail = req.UserEmail,
                CompanyName = req.CompanyName,
                ContactPerson = req.ContactPerson,
                Phone = req.Phone,
                Description = req.Description,
                Status = req.Status,
                AdminComment = req.AdminComment,
                CreatedAt = req.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminPartnerRequestListResponse
        {
            Items = items,
            Total = total,
            Page = page,
            Limit = limit,
            Pages = (total + limit - 1) / limit
        };
    }

    public async Task ApprovePartnerRequestAsync(string userEmail, CancellationToken cancellationToken)
    {
        var request = await _db.PartnerRequests.FirstOrDefaultAsync(x => x.UserEmail == userEmail, cancellationToken);
        if (request is null)
        {
            throw new HttpDetailException(404, "Заявка не найдена");
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == userEmail, cancellationToken);
        if (user is not null)
        {
            user.Role = UserRole.Partner;
            user.UpdatedAt = DateTime.UtcNow;
        }

        if (!await _db.Partners.AnyAsync(x => x.UserEmail == userEmail, cancellationToken))
        {
            _db.Partners.Add(new Partner
            {
                UserEmail = userEmail,
                CompanyName = request.CompanyName,
                Description = request.Description,
                IsApproved = true,
                AdsLimit = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _db.PartnerRequests.Remove(request);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminUserListResponse> GetUsersAsync(
        UserRole? role,
        string? search,
        int page,
        int limit,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        limit = Math.Clamp(limit, 1, 100);
        var query = _db.Users.Where(x => x.IsActive);
        if (role is not null)
        {
            query = query.Where(x => x.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(x => x.Email.ToLower().Contains(term) || x.FullName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(user => new AdminUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminUserListResponse
        {
            Items = items,
            Total = total,
            Page = page,
            Limit = limit,
            Pages = (total + limit - 1) / limit
        };
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new HttpDetailException(404, "Пользователь не найден");
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CategoryCreate data, CancellationToken cancellationToken)
    {
        if (await _db.Categories.AnyAsync(x => x.Name == data.Name, cancellationToken))
        {
            throw new HttpDetailException(400, "Категория уже существует");
        }

        var category = new Category { Name = data.Name, IsCustom = false };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return new CategoryResponse { Id = category.Id, Name = category.Name, IsCustom = category.IsCustom };
    }
}
