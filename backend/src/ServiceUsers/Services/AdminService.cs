using Microsoft.EntityFrameworkCore;
using ServiceUsers.Data;
using ServiceUsers.Data.Entities;
using ServiceUsers.Infrastructure;
using StudentPass.Contracts;

namespace ServiceUsers.Services;

public sealed class AdminService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _passwordHasher;

    public AdminService(AppDbContext db, PasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
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
                CreatedAt = req.CreatedAt,
                UpdatedAt = req.UpdatedAt
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

        if (request.Status != PartnerRequestStatus.Pending)
        {
            throw new HttpDetailException(400, "Заявка уже обработана");
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == userEmail, cancellationToken);
        if (user is not null)
        {
            user.Role = UserRole.Partner;
            user.UpdatedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(user.Phone))
            {
                user.Phone = request.Phone;
            }
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

        request.Status = PartnerRequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectPartnerRequestAsync(string userEmail, string comment, CancellationToken cancellationToken)
    {
        var request = await _db.PartnerRequests.FirstOrDefaultAsync(x => x.UserEmail == userEmail, cancellationToken);
        if (request is null)
        {
            throw new HttpDetailException(404, "Заявка не найдена");
        }

        if (request.Status != PartnerRequestStatus.Pending)
        {
            throw new HttpDetailException(400, "Заявка уже обработана");
        }

        request.Status = PartnerRequestStatus.Rejected;
        request.AdminComment = comment.Trim();
        request.UpdatedAt = DateTime.UtcNow;
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
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync(cancellationToken);

        await AttachManagerCompaniesAsync(items, cancellationToken);

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

    public async Task<AdminUserResponse> CreateManagerAsync(AdminManagerCreate data, CancellationToken cancellationToken)
    {
        var email = data.Email.Trim();
        if (await _db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new HttpDetailException(400, "Пользователь с таким email уже существует");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(data.Password),
            FullName = data.FullName.Trim(),
            Phone = data.Phone.Trim(),
            Role = UserRole.Manager,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    public async Task<AdminUserResponse> UpdateManagerAsync(int userId, AdminManagerUpdate data, CancellationToken cancellationToken)
    {
        var user = await RequireManagerUserAsync(userId, cancellationToken);
        user.FullName = data.FullName.Trim();
        user.Phone = data.Phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        var mapped = MapUser(user);
        await AttachManagerCompaniesAsync(new List<AdminUserResponse> { mapped }, cancellationToken);
        return mapped;
    }

    public async Task AssignPartnerAsync(int managerId, int partnerId, CancellationToken cancellationToken)
    {
        var manager = await RequireManagerUserAsync(managerId, cancellationToken);
        var partner = await _db.Partners.FirstOrDefaultAsync(x => x.Id == partnerId, cancellationToken);
        if (partner is null)
        {
            throw new HttpDetailException(404, "Компания не найдена");
        }

        var exists = await _db.ManagerAssignments.AnyAsync(
            x => x.ManagerEmail == manager.Email && x.PartnerId == partnerId,
            cancellationToken);
        if (exists)
        {
            throw new HttpDetailException(400, "Компания уже закреплена за этим менеджером");
        }

        _db.ManagerAssignments.Add(new ManagerAssignment
        {
            ManagerEmail = manager.Email,
            PartnerId = partner.Id,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnassignPartnerAsync(int managerId, int partnerId, CancellationToken cancellationToken)
    {
        var manager = await RequireManagerUserAsync(managerId, cancellationToken);
        var assignment = await _db.ManagerAssignments.FirstOrDefaultAsync(
            x => x.ManagerEmail == manager.Email && x.PartnerId == partnerId,
            cancellationToken);
        if (assignment is null)
        {
            throw new HttpDetailException(404, "Назначение не найдено");
        }

        _db.ManagerAssignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminCompanyResponse> CreatePartnerAsync(AdminPartnerCreate data, CancellationToken cancellationToken)
    {
        var email = data.Email.Trim();
        if (await _db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new HttpDetailException(400, "Пользователь с таким email уже существует");
        }

        User? manager = null;
        if (data.ManagerId is int managerId)
        {
            manager = await RequireManagerUserAsync(managerId, cancellationToken);
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(data.Password),
            FullName = data.CompanyName.Trim(),
            Phone = string.IsNullOrWhiteSpace(data.Phone) ? null : data.Phone.Trim(),
            Role = UserRole.Partner,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var partner = new Partner
        {
            UserEmail = email,
            CompanyName = data.CompanyName.Trim(),
            IsApproved = true,
            AdsLimit = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        _db.Partners.Add(partner);
        await _db.SaveChangesAsync(cancellationToken);

        if (manager is not null)
        {
            _db.ManagerAssignments.Add(new ManagerAssignment
            {
                ManagerEmail = manager.Email,
                PartnerId = partner.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new AdminCompanyResponse
        {
            Id = partner.Id,
            Company = partner.CompanyName,
            Email = partner.UserEmail,
            Manager = manager?.FullName ?? "",
            Discounts = 0,
            Clicks = 0,
            IsApproved = partner.IsApproved
        };
    }

    public async Task<List<AdminCompanyResponse>> GetCompaniesAsync(CancellationToken cancellationToken)
    {
        var partners = await _db.Partners.OrderBy(x => x.CompanyName).ToListAsync(cancellationToken);
        var assignments = await _db.ManagerAssignments
            .Select(x => new { x.PartnerId, x.Manager.FullName })
            .ToListAsync(cancellationToken);
        var emails = partners.Select(x => x.UserEmail).ToList();
        var stats = await LoadAdStatsAsync(emails, cancellationToken);
        var managersByPartner = assignments
            .GroupBy(x => x.PartnerId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.FullName).Distinct()));

        return partners.Select(partner =>
        {
            stats.TryGetValue(partner.UserEmail, out var stat);
            managersByPartner.TryGetValue(partner.Id, out var managerName);
            return new AdminCompanyResponse
            {
                Id = partner.Id,
                Company = partner.CompanyName,
                Email = partner.UserEmail,
                Manager = string.IsNullOrWhiteSpace(managerName) ? "—" : managerName,
                Discounts = stat.Discounts,
                Clicks = stat.Clicks,
                IsApproved = partner.IsApproved
            };
        }).ToList();
    }

    public async Task<AdminSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var usersCount = await _db.Users.CountAsync(x => x.IsActive, cancellationToken);
        var activeAdsCount = await _db.Ads.CountAsync(x => x.IsActive, cancellationToken);
        var pendingCount = await _db.PartnerRequests.CountAsync(x => x.Status == PartnerRequestStatus.Pending, cancellationToken);
        var partnersCount = await _db.Partners.CountAsync(x => x.IsApproved, cancellationToken);
        var totalClicks = await _db.Ads.SumAsync(x => (int?)x.ClicksCount, cancellationToken) ?? 0;

        var pending = await _db.PartnerRequests
            .Where(x => x.Status == PartnerRequestStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
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
                CreatedAt = req.CreatedAt,
                UpdatedAt = req.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var recentRequests = await _db.PartnerRequests
            .OrderByDescending(x => x.UpdatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);
        var recentUsers = await _db.Users
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => new { x.FullName, x.Email, x.Role, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var activity = new List<AdminActivityItem>();
        foreach (var request in recentRequests)
        {
            var (text, tone) = request.Status switch
            {
                PartnerRequestStatus.Approved => ($"Заявка {request.CompanyName} одобрена", "green"),
                PartnerRequestStatus.Rejected => ($"Заявка {request.CompanyName} отклонена", "purple"),
                _ => ($"{request.CompanyName} отправила заявку на партнёрство", "purple")
            };
            activity.Add(new AdminActivityItem
            {
                Text = text,
                CreatedAt = request.Status == PartnerRequestStatus.Pending ? request.CreatedAt : request.UpdatedAt,
                Tone = tone
            });
        }

        foreach (var user in recentUsers)
        {
            var label = user.Role switch
            {
                UserRole.Manager => "менеджером",
                UserRole.Partner => "партнёром",
                UserRole.Admin => "администратором",
                _ => "пользователем"
            };
            activity.Add(new AdminActivityItem
            {
                Text = $"{(string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName)} зарегистрирован как {label}",
                CreatedAt = user.CreatedAt,
                Tone = "blue"
            });
        }

        var topRaw = await _db.Ads
            .GroupBy(ad => ad.PartnerEmail)
            .Select(g => new
            {
                Email = g.Key,
                Clicks = g.Sum(ad => ad.ClicksCount),
                Discounts = g.Sum(ad => ad.IsActive ? 1 : 0)
            })
            .OrderByDescending(x => x.Clicks)
            .Take(5)
            .ToListAsync(cancellationToken);
        var topEmails = topRaw.Select(x => x.Email).ToList();
        var names = await _db.Partners
            .Where(x => topEmails.Contains(x.UserEmail))
            .ToDictionaryAsync(x => x.UserEmail, x => x.CompanyName, cancellationToken);

        return new AdminSummaryResponse
        {
            UsersCount = usersCount,
            ActiveAdsCount = activeAdsCount,
            PendingRequestsCount = pendingCount,
            PartnersCount = partnersCount,
            TotalClicks = totalClicks,
            PendingRequests = pending,
            Activity = activity.OrderByDescending(x => x.CreatedAt).Take(6).ToList(),
            TopCompanies = topRaw.Select(x => new AdminTopCompany
            {
                Company = names.TryGetValue(x.Email, out var name) ? name : x.Email,
                Clicks = x.Clicks,
                Discounts = x.Discounts
            }).ToList()
        };
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

    private async Task<User> RequireManagerUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new HttpDetailException(404, "Менеджер не найден");
        }

        if (user.Role != UserRole.Manager)
        {
            throw new HttpDetailException(400, "Пользователь не является менеджером");
        }

        return user;
    }

    private async Task AttachManagerCompaniesAsync(List<AdminUserResponse> users, CancellationToken cancellationToken)
    {
        var emails = users.Where(x => x.Role == UserRole.Manager).Select(x => x.Email).ToList();
        if (emails.Count == 0)
        {
            return;
        }

        var assignments = await _db.ManagerAssignments
            .Where(x => emails.Contains(x.ManagerEmail))
            .Select(x => new { x.ManagerEmail, x.Partner.Id, x.Partner.CompanyName, x.Partner.UserEmail })
            .ToListAsync(cancellationToken);
        var stats = await LoadAdStatsAsync(assignments.Select(x => x.UserEmail).Distinct().ToList(), cancellationToken);
        foreach (var user in users.Where(x => x.Role == UserRole.Manager))
        {
            user.Companies = assignments
                .Where(x => x.ManagerEmail == user.Email)
                .Select(x =>
                {
                    stats.TryGetValue(x.UserEmail, out var stat);
                    return new AdminAssignedCompanyResponse
                    {
                        Id = x.Id,
                        Name = x.CompanyName,
                        Discounts = stat.Discounts,
                        Clicks = stat.Clicks
                    };
                })
                .ToList();
        }
    }

    private async Task<Dictionary<string, (int Discounts, int Clicks)>> LoadAdStatsAsync(
        List<string> emails,
        CancellationToken cancellationToken)
    {
        if (emails.Count == 0)
        {
            return new Dictionary<string, (int Discounts, int Clicks)>();
        }

        var rows = await _db.Ads
            .Where(ad => emails.Contains(ad.PartnerEmail))
            .GroupBy(ad => ad.PartnerEmail)
            .Select(g => new
            {
                Email = g.Key,
                Discounts = g.Sum(ad => ad.IsActive ? 1 : 0),
                Clicks = g.Sum(ad => ad.ClicksCount)
            })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.Email, x => (x.Discounts, x.Clicks));
    }

    private static AdminUserResponse MapUser(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Phone = user.Phone,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
