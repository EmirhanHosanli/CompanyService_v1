using CompanyService.DATA;
using CompanyService.Models;
using CompanyService.Services;
//using CompanyService.Services.Repositories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using HealthChecks.SqlServer;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ---------- CONFIGURATION ----------
var config = builder.Configuration;
var env = builder.Environment.EnvironmentName;

// Connection strings
var connectionString = config.GetConnectionString("DefaultConnection")
    ?? "Server=db;Database=CompanyDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
var redisConnection = config.GetConnectionString("Redis") ?? "redis:6379";
var apiKey = config["ApiKey"] ?? "SuperSecretApiKey123";

// ---------- SERVICES ----------
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "CompanyAPI_";
});
builder.Services.Configure<CacheSettings>(config.GetSection("CacheSettings"));

builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<ICompanyService, CompanyService.Services.CompanyService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddHealthChecks().AddSqlServer(connectionString);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------- KESTREL ----------
builder.WebHost.UseUrls("http://*:80");

var app = builder.Build();

// ---------- MIDDLEWARE ----------
if (app.Environment.IsDevelopment() || env == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMetricServer();
app.UseHttpMetrics();

// API Key middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/health") || path.StartsWithSegments("/metrics") || path.StartsWithSegments("/swagger"))
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

// ---------- DATABASE MIGRATION ----------
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            Console.WriteLine("📦 Veritabanına bağlanılıyor...");
            dbContext.Database.Migrate();
            Console.WriteLine("✅ Migration başarılı!");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            Console.WriteLine($"❌ Hata: {ex.Message}. Tekrar denenecek... ({10 - retries}/10)");
            Thread.Sleep(5000);
        }
    }
}

app.Run();
