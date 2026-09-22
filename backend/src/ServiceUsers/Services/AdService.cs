using Microsoft.EntityFrameworkCore;
using ServiceUsers.Data;
using ServiceUsers.Data.Entities;
using ServiceUsers.Infrastructure;
using StudentPass.Contracts;

namespace ServiceUsers.Services;

public sealed class AdService
{
    private readonly AppDbContext _db;

    public AdService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdListResponse> GetAdsAsync(
        string? category,
        string? search,
        string? sort,
        int page,
        int limit,
        string? userEmail,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        limit = Math.Clamp(limit, 1, 100);

        var query = _db.Ads
            .Include(x => x.Categories)
            .Include(x => x.Partner)
            .Where(x => x.IsActive && x.EndDate > DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Categories.Any(c => c.Name == category));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Title.ToLower().Contains(search.ToLower()));
        }

        query = sort switch
        {
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            "ending_soon" => query.OrderBy(x => x.EndDate),
            "popular" => query.OrderByDescending(x => x.ClicksCount),
            _ => query.OrderByDescending(x => x.Prioritet).ThenByDescending(x => x.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var ads = await query.Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);

        var favorites = new HashSet<int>();
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            favorites = (await _db.Favorites.Where(x => x.UserEmail == userEmail).Select(x => x.AdId).ToListAsync(cancellationToken)).ToHashSet();
        }

        var items = ads.Select(ad => MapAd(ad, favorites.Contains(ad.Id))).ToList();
        var pages = (total + limit - 1) / limit;
        return new AdListResponse { Items = items, Total = total, Page = page, Limit = limit, Pages = pages };
    }

    public async Task<List<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _db.Categories
            .OrderBy(x => x.Name)
            .Select(x => new CategoryResponse { Id = x.Id, Name = x.Name, IsCustom = x.IsCustom })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdDetailResponse> GetAdAsync(int adId, string? userEmail, CancellationToken cancellationToken)
    {
        var ad = await _db.Ads
            .Include(x => x.Categories)
            .Include(x => x.Partner)
            .FirstOrDefaultAsync(x => x.Id == adId && x.IsActive && x.EndDate > DateTime.UtcNow, cancellationToken);

        if (ad is null)
        {
            throw new HttpDetailException(404, "Объявление не найдено");
        }

        var isFavorite = false;
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            isFavorite = await _db.Favorites.AnyAsync(x => x.UserEmail == userEmail && x.AdId == adId, cancellationToken);
        }

        var mapped = MapAd(ad, isFavorite);
        return new AdDetailResponse
        {
            Id = mapped.Id,
            Title = mapped.Title,
            Description = mapped.Description,
            DiscountPercent = mapped.DiscountPercent,
            Url = mapped.Url,
            Address = mapped.Address,
            EndDate = mapped.EndDate,
            ClicksCount = mapped.ClicksCount,
            PartnerEmail = mapped.PartnerEmail,
            PartnerName = mapped.PartnerName,
            Categories = mapped.Categories,
            IsFavorite = mapped.IsFavorite,
            EmodziId = mapped.EmodziId,
            Prioritet = mapped.Prioritet,
            CreatedAt = ad.CreatedAt,
            UpdatedAt = ad.UpdatedAt
        };
    }

    public async Task<string> ClickAdAsync(int adId, CancellationToken cancellationToken)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId, cancellationToken);
        if (ad is null)
        {
            throw new HttpDetailException(404, "Объявление не найдено");
        }

        ad.ClicksCount += 1;
        await _db.SaveChangesAsync(cancellationToken);
        return ad.Url;
    }

    public async Task<FavoriteListResponse> GetFavoritesAsync(string userEmail, int page, int limit, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        limit = Math.Clamp(limit, 1, 100);
        var query = _db.Ads
            .Include(x => x.Partner)
            .Include(x => x.Categories)
            .Join(_db.Favorites.Where(f => f.UserEmail == userEmail),
                ad => ad.Id,
                fav => fav.AdId,
                (ad, fav) => new { ad, fav })
            .Where(x => x.ad.IsActive && x.ad.EndDate > DateTime.UtcNow)
            .OrderByDescending(x => x.fav.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);
        return new FavoriteListResponse
        {
            Items = rows.Select(x => new FavoriteResponse
            {
                AdId = x.ad.Id,
                Title = x.ad.Title,
                DiscountPercent = x.ad.DiscountPercent,
                EndDate = x.ad.EndDate,
                PartnerName = x.ad.Partner.CompanyName
            }).ToList(),
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    public async Task AddFavoriteAsync(string userEmail, int adId, CancellationToken cancellationToken)
    {
        var ad = await _db.Ads.FirstOrDefaultAsync(x => x.Id == adId && x.IsActive, cancellationToken);
        if (ad is null)
        {
            throw new HttpDetailException(404, "Объявление не найдено");
        }

        if (await _db.Favorites.AnyAsync(x => x.UserEmail == userEmail && x.AdId == adId, cancellationToken))
        {
            throw new HttpDetailException(400, "Уже в избранном");
        }

        _db.Favorites.Add(new Favorite { UserEmail = userEmail, AdId = adId, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFavoriteAsync(string userEmail, int adId, CancellationToken cancellationToken)
    {
        var favorite = await _db.Favorites.FirstOrDefaultAsync(x => x.UserEmail == userEmail && x.AdId == adId, cancellationToken);
        if (favorite is null)
        {
            throw new HttpDetailException(404, "Объявление не найдено в избранном");
        }

        _db.Favorites.Remove(favorite);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public static AdResponse MapAd(Ad ad, bool isFavorite = false) => new()
    {
        Id = ad.Id,
        Title = ad.Title,
        Description = ad.Description,
        DiscountPercent = ad.DiscountPercent,
        Url = ad.Url,
        Address = ad.Address,
        EndDate = ad.EndDate,
        ClicksCount = ad.ClicksCount,
        PartnerEmail = ad.PartnerEmail,
        PartnerName = ad.Partner?.CompanyName ?? string.Empty,
        Categories = ad.Categories.Select(c => c.Name).ToList(),
        IsFavorite = isFavorite,
        EmodziId = ad.EmodziId,
        Prioritet = ad.Prioritet
    };
}
