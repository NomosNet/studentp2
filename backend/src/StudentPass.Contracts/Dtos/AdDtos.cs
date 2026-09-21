using System.ComponentModel.DataAnnotations;

namespace StudentPass.Contracts;

public sealed class CategoryCreate
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;
}

public sealed class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
}

public sealed class AdCreate
{
    [Required, MinLength(1)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, 100)]
    public int DiscountPercent { get; set; }

    [Required, MinLength(1)]
    public string Url { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public DateTime EndDate { get; set; }

    [Required, MinLength(1)]
    public List<int> CategoryIds { get; set; } = new();

    public int? EmodziId { get; set; }
    public int Prioritet { get; set; }
}

public sealed class AdUpdate
{
    public string? Title { get; set; }
    public string? Description { get; set; }

    [Range(1, 100)]
    public int? DiscountPercent { get; set; }

    public string? Url { get; set; }
    public string? Address { get; set; }
    public DateTime? EndDate { get; set; }
    public List<int>? CategoryIds { get; set; }
    public int? EmodziId { get; set; }
    public int? Prioritet { get; set; }
}

public class AdResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DiscountPercent { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int ClicksCount { get; set; }
    public string PartnerEmail { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public bool IsFavorite { get; set; }
    public int? EmodziId { get; set; }
    public int Prioritet { get; set; }
}

public sealed class AdDetailResponse : AdResponse
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class AdListResponse
{
    public List<AdResponse> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Pages { get; set; }
}

public sealed class FavoriteResponse
{
    public int AdId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DiscountPercent { get; set; }
    public DateTime EndDate { get; set; }
    public string PartnerName { get; set; } = string.Empty;
}

public sealed class FavoriteListResponse
{
    public List<FavoriteResponse> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
}
