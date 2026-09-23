using System.ComponentModel.DataAnnotations;

namespace StudentPass.Contracts;

public sealed class AdminUserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AdminAssignedCompanyResponse> Companies { get; set; } = new();
}

public sealed class AdminAssignedCompanyResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Discounts { get; set; }
    public int Clicks { get; set; }
}

public sealed class AdminUserListResponse
{
    public List<AdminUserResponse> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Pages { get; set; }
}

public sealed class AdminPartnerRequestListResponse
{
    public List<PartnerRequestResponse> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Pages { get; set; }
}

public sealed class AdminManagerCreate
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Phone { get; set; } = string.Empty;
}

public sealed class AdminManagerUpdate
{
    [Required, MinLength(1)]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Phone { get; set; } = string.Empty;
}

public sealed class AdminPartnerCreate
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string CompanyName { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public int? ManagerId { get; set; }
}

public sealed class RejectPartnerRequest
{
    [Required, MinLength(1)]
    public string Comment { get; set; } = string.Empty;
}

public sealed class AdminCompanyResponse
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public int Discounts { get; set; }
    public int Clicks { get; set; }
    public bool IsApproved { get; set; }
}

public sealed class AdminActivityItem
{
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Tone { get; set; } = "blue";
}

public sealed class AdminTopCompany
{
    public string Company { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public int Discounts { get; set; }
}

public sealed class AdminSummaryResponse
{
    public int UsersCount { get; set; }
    public int ActiveAdsCount { get; set; }
    public int PendingRequestsCount { get; set; }
    public int PartnersCount { get; set; }
    public int TotalClicks { get; set; }
    public List<PartnerRequestResponse> PendingRequests { get; set; } = new();
    public List<AdminActivityItem> Activity { get; set; } = new();
    public List<AdminTopCompany> TopCompanies { get; set; } = new();
}

public sealed class ManagerCompanyResponse
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AdsCount { get; set; }
    public int Clicks { get; set; }
}
