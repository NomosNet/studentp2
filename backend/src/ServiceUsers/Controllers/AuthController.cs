using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceUsers.Data.Entities;
using ServiceUsers.Services;
using StudentPass.Contracts;

namespace ServiceUsers.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly JwtTokenService _jwt;
    private readonly CurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    public AuthController(AuthService auth, JwtTokenService jwt, CurrentUserService currentUser, IConfiguration configuration)
    {
        _auth = auth;
        _jwt = jwt;
        _currentUser = currentUser;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("send_code")]
    public async Task<ActionResult<MessageResponse>> SendCode([FromBody] SendCodeRequest request, CancellationToken cancellationToken)
    {
        await _auth.SendCodeAsync(request.Email, cancellationToken);
        return Ok(new MessageResponse { Message = "Код подтверждения отправлен на почту" });
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<MessageResponse>> Register([FromBody] UserRegister data, CancellationToken cancellationToken)
    {
        var message = await _auth.RegisterAsync(data, cancellationToken);
        return Ok(new MessageResponse { Message = message });
    }

    [AllowAnonymous]
    [HttpPost("register-partner")]
    public async Task<ActionResult<MessageResponse>> RegisterPartner([FromBody] UserRegisterPartner data, CancellationToken cancellationToken)
    {
        await _auth.RegisterPartnerAsync(data, cancellationToken);
        return Ok(new MessageResponse
        {
            Message = "Заявка на партнёрство отправлена. После одобрения администратором Вы сможете размещать объявления!"
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] UserLogin data, CancellationToken cancellationToken)
    {
        var (user, token) = await _auth.LoginAsync(data, cancellationToken);
        AppendAccessCookie(token);
        return Ok(new TokenResponse { AccessToken = token });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public ActionResult<MessageResponse> Logout()
    {
        Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
        return Ok(new MessageResponse { Message = "Выход выполнен" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetRequiredUserAsync(cancellationToken);
        return Ok(MapUser(user));
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<ActionResult<MessageResponse>> DeleteMe([FromBody] UserLogin data, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetRequiredUserAsync(cancellationToken);
        await _auth.DeleteMeAsync(user, data, cancellationToken);
        Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
        return Ok(new MessageResponse { Message = "Аккаунт удалён. Данные будут храниться 3 месяца" });
    }

    [AllowAnonymous]
    [HttpPost("recover_account")]
    public async Task<ActionResult<MessageResponse>> Recover([FromBody] UserLogin data, CancellationToken cancellationToken)
    {
        var message = await _auth.RecoverAccountAsync(data, cancellationToken);
        return Ok(new MessageResponse { Message = message });
    }

    private void AppendAccessCookie(string token)
    {
        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = _configuration.GetValue("COOKIE_SECURE", false),
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(_jwt.ExpiresMinutes),
            Path = "/"
        });
    }

    private static UserResponse MapUser(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
