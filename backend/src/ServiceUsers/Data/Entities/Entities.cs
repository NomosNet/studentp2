using StudentPass.Contracts;

namespace ServiceUsers.Data.Entities;

public sealed class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Partner? Partner { get; set; }
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}

public sealed class EmailVerification
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PartnerRequest
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PartnerRequestStatus Status { get; set; } = PartnerRequestStatus.Pending;
    public string? AdminComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Partner
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsApproved { get; set; } = true;
    public int AdsLimit { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Ad> Ads { get; set; } = new List<Ad>();
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
    public ICollection<Ad> Ads { get; set; } = new List<Ad>();
}

public sealed class Ad
{
    public int Id { get; set; }
    public string PartnerEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DiscountPercent { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int ClicksCount { get; set; }
    public bool IsActive { get; set; } = true;
    public int? EmodziId { get; set; }
    public int Prioritet { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Partner Partner { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}

public sealed class AdCategory
{
    public int AdId { get; set; }
    public int CategoryId { get; set; }
}

public sealed class Favorite
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int AdId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Ad Ad { get; set; } = null!;
}
