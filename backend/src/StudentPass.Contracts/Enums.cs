namespace StudentPass.Contracts;

public enum UserRole
{
    User,
    Admin,
    Partner,
    Manager
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
        UserRole.Manager => "manager",
        _ => "user"
    };

    public static UserRole FromApi(string? value) => (value ?? "").Trim().ToLowerInvariant() switch
    {
        "admin" => UserRole.Admin,
        "partner" => UserRole.Partner,
        "manager" => UserRole.Manager,
        _ => UserRole.User
    };
}
