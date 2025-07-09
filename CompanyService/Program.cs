using CompanyService.DATA;
using CompanyService.Models;
using CompanyService.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using HealthChecks.SqlServer;
using StackExchange.Redis;
using Fleck;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
var env = builder.Environment.EnvironmentName;

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? "Server=db;Database=CompanyDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
var redisConnection = config.GetConnectionString("Redis") ?? "redis:6379";
var apiKey = config["ApiKey"] ?? "SuperSecretApiKey123";

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var redisConfig = new ConfigurationOptions
{
    EndPoints = { redisConnection },
    AbortOnConnectFail = false,
    ConnectRetry = 3,
    ConnectTimeout = 5000,
    KeepAlive = 180,
    DefaultDatabase = 0
};

var serviceProvider = builder.Services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var multiplexer = ConnectionMultiplexer.Connect(redisConfig);

int retryCount = 0;

multiplexer.ConnectionFailed += (sender, args) =>
{
    retryCount++;
    logger.LogError($"[Retry {retryCount}/{redisConfig.ConnectRetry}] Failed to connect to Redis: {args.Exception?.Message}");
};

multiplexer.ConnectionRestored += (sender, args) =>
{
    retryCount = 0;
    logger.LogInformation("[Info] Redis connection restored.");
};

builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

builder.Services.Configure<CacheSettings>(config.GetSection("CacheSettings"));
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<ICompanyService, CompanyService.Services.CompanyService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddHealthChecks().AddSqlServer(connectionString);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<WebSocketNotifier>();

builder.WebHost.UseUrls("http://*:80");

var app = builder.Build();

if (app.Environment.IsDevelopment() || env == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMetricServer();
app.UseHttpMetrics();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    if (!string.IsNullOrEmpty(path) &&
        (path.Contains("health", StringComparison.OrdinalIgnoreCase) ||
         path.Contains("swagger", StringComparison.OrdinalIgnoreCase)))
    {
        await next();
        return;
    }

    if (!context.Request.Headers.TryGetValue("X-API-KEY", out var providedKey) || providedKey != apiKey)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized - Invalid API Key");
        return;
    }

    await next();
});

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            results = report.Entries.Select(e => new { key = e.Key, value = e.Value.Status.ToString() })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();
