using StackExchange.Redis;

namespace ApiGateway.Middleware;

public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly IConnectionMultiplexer? _redis;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, IServiceProvider services)
    {
        _next = next;
        _logger = logger;
        _redis = services.GetService<IConnectionMultiplexer>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method) || !ShouldLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (_redis is null)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";
        var (limit, window) = GetLimit(path);
        var ip = GetClientIp(context);
        var keyReqs = $"{ip}:{path}:requests";
        var keyBlocking = $"{ip}:{path}:blocking";

        try
        {
            var db = _redis.GetDatabase();
            var blockingTtl = await db.KeyTimeToLiveAsync(keyBlocking);
            if (await db.KeyExistsAsync(keyBlocking) && blockingTtl is { TotalSeconds: > 0 })
            {
                await WriteLimited(context, (int)blockingTtl.Value.TotalSeconds, limit, window);
                return;
            }

            var current = await db.StringGetAsync(keyReqs);
            if (current.IsNull)
            {
                await db.StringSetAsync(keyReqs, 1, TimeSpan.FromSeconds(window));
            }
            else
            {
                var count = (int)current;
                if (count >= limit - 1)
                {
                    await db.KeyExpireAsync(keyBlocking, TimeSpan.FromSeconds(window));
                    await db.KeyDeleteAsync(keyReqs);
                    await WriteLimited(context, window, limit, window);
                    return;
                }

                await db.StringIncrementAsync(keyReqs);
            }

            var response = context.Response;
            response.OnStarting(() =>
            {
                response.Headers["X-RateLimit-Limit"] = limit.ToString();
                return Task.CompletedTask;
            });
            await _next(context);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error, skipping rate limit");
            await _next(context);
        }
    }

    private static bool ShouldLimit(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return !(value.StartsWith("/health")
            || value.StartsWith("/api/health")
            || value.StartsWith("/api/v1/health")
            || value.StartsWith("/favicon.ico"));
    }

    private static (int Limit, int Window) GetLimit(string path) => path switch
    {
        "/api/v1/auth/login" or "/api/v1/users/login" => (500, 60),
        "/api/v1/auth/register" or "/api/v1/users/register" => (500, 300),
        _ => (1000, 60)
    };

    private static string GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            return forwarded.ToString().Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static Task WriteLimited(HttpContext context, int remaining, int limit, int window)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        return context.Response.WriteAsJsonAsync(new
        {
            detail = $"Слишком много запросов. Лимит: {limit} в {window} секунд. Попробуйте через {remaining} сек."
        });
    }
}
