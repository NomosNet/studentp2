namespace StudentPass.Contracts;

public sealed class AdminUserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
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
