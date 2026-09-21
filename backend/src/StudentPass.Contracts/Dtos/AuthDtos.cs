using System.ComponentModel.DataAnnotations;

namespace StudentPass.Contracts;

public sealed class SendCodeRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class UserRegister
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public int Code { get; set; }
}

public sealed class UserRegisterPartner
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Phone { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed class UserLogin
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
