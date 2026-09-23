namespace StudentPass.Contracts;

public sealed class PartnerResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public int AdsCount { get; set; }
}

public sealed class PartnerAdsResponse
{
    public List<AdResponse> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Pages { get; set; }
    public int AdsUsed { get; set; }
    public int AdsLimit { get; set; }
}

public sealed class PartnerRequestResponse
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PartnerRequestStatus Status { get; set; }
    public string? AdminComment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
