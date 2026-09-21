namespace StudentPass.Contracts;

public enum UserRole
{
    User,
    Admin,
    Partner
}

public enum PartnerRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public static class UserRoleExtensions
{
    public static string ToApi(this UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Partner => "partner",
        _ => "user"
    };

    public static UserRole FromApi(string? value) => (value ?? "").Trim().ToLowerInvariant() switch
    {
        "admin" => UserRole.Admin,
        "partner" => UserRole.Partner,
        _ => UserRole.User
    };
}
