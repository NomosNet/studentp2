using ApiGateway.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var redisUrl = builder.Configuration["REDIS_URL"] ?? builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisUrl))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(ToRedisConfig(redisUrl)));
}

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<JwtProxyMiddleware>();

app.MapGet("/health", () => Results.Json(new { status = "API Gateway is healthy" }));
app.MapGet("/api/health", () => Results.Json(new { status = "API Gateway API is healthy" }));
app.MapReverseProxy();

app.Run();

static string ToRedisConfig(string url)
{
    if (url.StartsWith("redis://", StringComparison.OrdinalIgnoreCase))
    {
        return url["redis://".Length..];
    }

    return url;
}
