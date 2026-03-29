using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProVantage.API.Middleware;
using ProVantage.Application;
using ProVantage.Infrastructure;
using ProVantage.Infrastructure.Jobs;
using ProVantage.Infrastructure.Persistence;
using ProVantage.Infrastructure.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var useSqlite = builder.Configuration.GetValue<bool>("Testing:UseSqlite");
var useInMemoryCache = builder.Configuration.GetValue<bool>("Testing:UseInMemoryCache");
var disableBackgroundJobs = builder.Configuration.GetValue<bool>("Testing:DisableBackgroundJobs");

// ──────────────────────────────────────────────
// SERILOG
// ──────────────────────────────────────────────
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// ──────────────────────────────────────────────
// CLEAN ARCHITECTURE DI REGISTRATION
// ──────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ──────────────────────────────────────────────
// AUTHENTICATION (JWT Bearer)
// ──────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };

    // Allow SignalR to receive token via query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ──────────────────────────────────────────────
// RATE LIMITING
// ──────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Auth endpoints: 5 requests per minute
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // General API: 100 requests per minute with sliding window
    options.AddSlidingWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 4;
        opt.QueueLimit = 10;
    });

    // Report/analytics: expensive queries, token bucket
    options.AddTokenBucketLimiter("reports", opt =>
    {
        opt.TokenLimit = 10;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(30);
        opt.TokensPerPeriod = 2;
        opt.QueueLimit = 5;
    });
});

// ──────────────────────────────────────────────
// OUTPUT CACHING
// ──────────────────────────────────────────────
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b => b.NoCache());
    options.AddPolicy("Dashboard", b =>
        b.Expire(TimeSpan.FromMinutes(1)).Tag("dashboard"));
    options.AddPolicy("Analytics", b =>
        b.Expire(TimeSpan.FromSeconds(60)).Tag("analytics"));
    options.AddPolicy("VendorList", b =>
        b.Expire(TimeSpan.FromSeconds(30))
         .SetVaryByQuery("page", "pageSize", "search", "status")
         .Tag("vendors"));
});

// ──────────────────────────────────────────────
// CORS
// ──────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        var origins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

// ──────────────────────────────────────────────
// CONTROLLERS + SWAGGER
// ──────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProVantage API",
        Version = "v1",
        Description = "Enterprise Procurement & Vendor Management Platform"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ──────────────────────────────────────────────
// HEALTH CHECKS
// ──────────────────────────────────────────────
var healthChecks = builder.Services.AddHealthChecks();

if (useSqlite)
{
    healthChecks.AddCheck(
        "sqlite",
        () => HealthCheckResult.Healthy("SQLite testing mode"),
        tags: ["db", "ready"]);
}
else
{
    healthChecks.AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        tags: ["db", "ready"]);
}

if (!useInMemoryCache)
{
    healthChecks.AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "redis",
        tags: ["cache", "ready"]);
}

// ══════════════════════════════════════════════
// MIDDLEWARE PIPELINE
// ══════════════════════════════════════════════
var app = builder.Build();

// Correlation ID first — so all logs have it
app.UseMiddleware<RequestCorrelationMiddleware>();

// Global error handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Serilog request logging
app.UseSerilogRequestLogging();

// Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ProVantage API v1");
        options.DocumentTitle = "ProVantage API";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

// Tenant resolution after auth (needs JWT claims)
app.UseMiddleware<TenantResolutionMiddleware>();

// Controllers
app.MapControllers();

// SignalR hubs
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<DashboardHub>("/hubs/dashboard");

if (!disableBackgroundJobs)
{
    // Hangfire dashboard (dev only — lock down in production)
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "ProVantage Jobs",
        Authorization = [] // Open in dev; add auth filter for production
    });
}

// Health checks
app.MapHealthChecks("/health");

if (!disableBackgroundJobs)
{
    // Register recurring jobs
    RecurringJob.AddOrUpdate<ContractExpiryJob>(
        "contract-expiry-check",
        job => job.ExecuteAsync(),
        "0 8 * * *"); // daily at 08:00 UTC

    RecurringJob.AddOrUpdate<SlaEscalationJob>(
        "sla-escalation-check",
        job => job.ExecuteAsync(),
        "0 * * * *"); // every hour
}

// Seed database in development
if (app.Environment.IsDevelopment())
{
    await SeedData.SeedAsync(app.Services);
}

await app.RunAsync();

public partial class Program
{
}
