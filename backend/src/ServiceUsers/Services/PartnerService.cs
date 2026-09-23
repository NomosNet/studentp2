using Microsoft.EntityFrameworkCore;
using ServiceUsers.Data;
using ServiceUsers.Data.Entities;
using ServiceUsers.Infrastructure;
using StudentPass.Contracts;

namespace ServiceUsers.Services;

public sealed class PartnerService
{
    private readonly AppDbContext _db;

    public PartnerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Partner> RequireApprovedPartnerAsync(string email, CancellationToken cancellationToken)
    {
        var partner = await _db.Partners.FirstOrDefaultAsync(x => x.UserEmail == email, cancellationToken);
        if (partner is null || !partner.IsApproved)
        {
            throw new HttpDetailException(403, "Партнёр не одобрен администратором");
        }

        return partner;
    }

    public async Task<List<PartnerResponse>> GetPartnersAsync(string? search, int limit, CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 100);
        var query = _db.Partners.Where(x => x.IsApproved);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.CompanyName.ToLower().Contains(search.ToLower()));
        }

        var partners = await query.OrderByDescending(x => x.CreatedAt).Take(limit).ToListAsync(cancellationToken);
        var result = new List<PartnerResponse>();
        foreach (var partner in partners)
        {
            var adsCount = await _db.Ads.CountAsync(x => x.PartnerEmail == partner.UserEmail && x.IsActive, cancellationToken);
            result.Add(new PartnerResponse
            {
                Id = partner.Id,
                Email = partner.UserEmail,
                CompanyName = partner.CompanyName,
                Description = partner.Description,
                LogoUrl = partner.LogoUrl,
                AdsCount = adsCount
            });
        }

        return result;
    }

    public async Task<List<ManagerCompanyResponse>> GetManagedCompaniesAsync(string managerEmail, CancellationToken cancellationToken)
    {
        var partners = await _db.ManagerAssignments
            .Where(x => x.ManagerEmail == managerEmail && x.Partner.IsApproved)
            .Select(x => x.Partner)
            .OrderBy(x => x.CompanyName)
            .ToListAsync(cancellationToken);
        var emails = partners.Select(x => x.UserEmail).ToList();
        var stats = emails.Count == 0
            ? new Dictionary<string, (int Ads, int Clicks)>()
            : (await _db.Ads
                .Where(ad => emails.Contains(ad.PartnerEmail))
                .GroupBy(ad => ad.PartnerEmail)
                .Select(g => new
                {
                    Email = g.Key,
                    Ads = g.Sum(ad => ad.IsActive ? 1 : 0),
                    Clicks = g.Sum(ad => ad.ClicksCount)
                })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.Email, x => (x.Ads, x.Clicks));

        return partners.Select(partner =>
        {
            stats.TryGetValue(partner.UserEmail, out var stat);
            return new ManagerCompanyResponse
            {
                Id = partner.Id,
                CompanyName = partner.CompanyName,
                Email = partner.UserEmail,
                AdsCount = stat.Ads,
                Clicks = stat.Clicks
            };
        }).ToList();
    }

    public async Task<AdListResponse> GetPartnerAdsPublicAsync(int partnerId, int page, int limit, CancellationToken cancellationToken)
    {
        var partner = await _db.Partners.FirstOrDefaultAsync(x => x.Id == partnerId && x.IsApproved, cancellationToken);
        if (partner is null)
        {
            throw new HttpDetailException(404, "Партнёр не найден");
        }

        return await GetAdsByPartnerAsync(partner, page, limit, includeInactive: false, cancellationToken);
    }

    public async Task<PartnerAdsResponse> GetMyAdsAsync(Partner partner, int page, int limit, CancellationToken cancellationToken)
    {
        var list = await GetAdsByPartnerAsync(partner, page, limit, includeInactive: true, cancellationToken);
        var adsUsed = await _db.Ads.CountAsync(x => x.PartnerEmail == partner.UserEmail && x.IsActive, cancellationToken);
        return new PartnerAdsResponse
        {
            Items = list.Items,
            Total = list.Total,
            Page = list.Page,
            Limit = list.Limit,
            Pages = list.Pages,
            AdsUsed = adsUsed,
            AdsLimit = partner.AdsLimit
        };
    }

    public async Task CreateAdAsync(Partner partner, AdCreate data, CancellationToken cancellationToken)
    {
        var currentCount = await _db.Ads.CountAsync(x => x.PartnerEmail == partner.UserEmail && x.IsActive, cancellationToken);
        if (currentCount >= partner.AdsLimit)
        {
            throw new HttpDetailException(400, $"Превышен лимит объявлений (максимум {partner.AdsLimit})");
        }

        var categories = await _db.Categories.Where(x => data.CategoryIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (categories.Count != data.CategoryIds.Distinct().Count())
        {
            throw new HttpDetailException(400, "Категория не найдена");
        }

        var ad = new Ad
        {
            PartnerEmail = partner.UserEmail,
            Title = data.Title,
            Description = data.Description,
            DiscountPercent = data.DiscountPercent,
            Url = data.Url,
            Address = data.Address,
            EndDate = DateTime.SpecifyKind(data.EndDate, DateTimeKind.Utc),
            EmodziId = data.EmodziId,
            Prioritet = data.Prioritet,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Categories = categories
        };
        _db.Ads.Add(ad);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAdAsync(Partner partner, int adId, AdUpdate data, CancellationToken cancellationToken)
    {
        var ad = await _db.Ads.Include(x => x.Categories).FirstOrDefaultAsync(x => x.Id == adId, cancellationToken);
        if (ad is null || ad.PartnerEmail != partner.UserEmail)
        {
            throw new HttpDetailException(404, "Объявление не найдено");
        }

        if (data.Title is not null) ad.Title = data.Title;
        if (data.Description is not null) ad.Description = data.Description;
        if (data.DiscountPercent is not null) ad.DiscountPercent = data.DiscountPercent.Value;
        if (data.Url is not null) ad.Url = data.Url;
        if (data.Address is not null) ad.Address = data.Address;
        if (data.EndDate is not null) ad.EndDate = DateTime.SpecifyKind(data.EndDate.Value, DateTimeKind.Utc);
        if (data.EmodziId is not null) ad.EmodziId = data.EmodziId;
        if (data.Prioritet is not null) ad.Prioritet = data.Prioritet.Value;
        if (data.CategoryIds is not null)
        {
            var categories = await _db.Categories.Where(x => data.CategoryIds.Contains(x.Id)).ToListAsync(cancellationToken);
            if (categories.Count != data.CategoryIds.Distinct().Count())
            {
                throw new HttpDetailException(400, "Категория не найдена");
            }

            ad.Categories.Clear();
            foreach (var category in categories)
            {
                ad.Categories.Add(category);
            }
        }

        ad.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAdAsync(Partner partner, int adId, CancellationToken cancellationToken)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId, cancellationToken);
        if (ad is null || ad.PartnerEmail != partner.UserEmail)
        {
            throw new HttpDetailException(404, "Объявление не найдено");
        }

        _db.Ads.Remove(ad);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdListResponse> GetAdsByPartnerAsync(
        Partner partner,
        int page,
        int limit,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        limit = Math.Clamp(limit, 1, 100);
        var query = _db.Ads.Include(x => x.Categories).Where(x => x.PartnerEmail == partner.UserEmail);
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var total = await query.CountAsync(cancellationToken);
        var ads = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);
        var pages = (total + limit - 1) / limit;
        return new AdListResponse
        {
            Items = ads.Select(ad =>
            {
                ad.Partner = partner;
                return AdService.MapAd(ad);
            }).ToList(),
            Total = total,
            Page = page,
            Limit = limit,
            Pages = pages
        };
    }
}
