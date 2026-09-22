using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Middleware;

public sealed class JwtProxyMiddleware
{
    private static readonly Regex AdId = new(@"^/api/v1/ads/\d+$", RegexOptions.Compiled);
    private static readonly Regex AdClick = new(@"^/api/v1/ads/\d+/click$", RegexOptions.Compiled);
    private static readonly Regex PartnerAds = new(@"^/api/v1/partners/\d+/ads$", RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly TokenValidationParameters _parameters;
    private readonly ILogger<JwtProxyMiddleware> _logger;

    public JwtProxyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<JwtProxyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        var secret = configuration["JWT_SECRET_KEY"] ?? configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT_SECRET_KEY is not configured");
        _parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method) || IsPublic(context.Request))
        {
            await _next(context);
            return;
        }

        var token = context.Request.Cookies["access_token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = header["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            await WriteUnauthorized(context, "Unauthorized");
            return;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(token, _parameters, out _);
            var userId = principal.FindFirst("user_id")?.Value ?? string.Empty;
            var email = principal.FindFirst("sub")?.Value ?? string.Empty;
            var role = principal.FindFirst("role")?.Value ?? string.Empty;
            context.Request.Headers["X-User-Id"] = userId;
            context.Request.Headers["X-User-Email"] = email;
            context.Request.Headers["X-User-Roles"] = role;
            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            await WriteUnauthorized(context, "Token expired");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT validation error");
            await WriteUnauthorized(context, "Invalid token");
        }
    }

    private static bool IsPublic(HttpRequest request)
    {
        var path = request.Path.Value?.TrimEnd('/') ?? string.Empty;
        if (path.Length == 0)
        {
            path = "/";
        }

        if (path is "/health" or "/api/health" or "/api/v1/health")
        {
            return true;
        }

        if (path is "/api/v1/auth/login"
            or "/api/v1/auth/register"
            or "/api/v1/auth/register-partner"
            or "/api/v1/auth/send_code"
            or "/api/v1/auth/recover_account"
            or "/api/v1/auth/logout")
        {
            return true;
        }

        if (HttpMethods.IsGet(request.Method))
        {
            if (path is "/api/v1/ads" or "/api/v1/ads/categories" or "/api/v1/partners")
            {
                return true;
            }

            if (AdId.IsMatch(path) || PartnerAds.IsMatch(path))
            {
                return true;
            }
        }

        return HttpMethods.IsPost(request.Method) && AdClick.IsMatch(path);
    }

    private static Task WriteUnauthorized(HttpContext context, string error)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new { error });
    }
}
