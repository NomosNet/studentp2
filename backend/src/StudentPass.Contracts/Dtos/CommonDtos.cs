namespace StudentPass.Contracts;

public sealed class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "bearer";
}
