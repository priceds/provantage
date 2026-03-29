using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProVantage.Domain.Interfaces;
using ProVantage.Infrastructure.Jobs;
using ProVantage.Infrastructure.Persistence;
using ProVantage.Infrastructure.Persistence.Interceptors;
using ProVantage.Infrastructure.Services;
using StackExchange.Redis;

namespace ProVantage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var useSqlite = configuration.GetValue<bool>("Testing:UseSqlite");
        var useInMemoryCache = configuration.GetValue<bool>("Testing:UseInMemoryCache");
        var disableBackgroundJobs = configuration.GetValue<bool>("Testing:DisableBackgroundJobs");

        // EF Core + SQL Server
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (useSqlite)
            {
                options.UseSqlite(connectionString)
                    .AddInterceptors(auditInterceptor, softDeleteInterceptor);
            }
            else
            {
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                    })
                    .AddInterceptors(auditInterceptor, softDeleteInterceptor);
            }
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        if (useInMemoryCache)
        {
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }
        else
        {
            // Redis
            var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnection));
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "ProVantage:";
            });
            services.AddScoped<ICacheService, RedisCacheService>();
        }

        // Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<DatabaseSeeder>();

        // Notification service (SignalR + DB)
        services.AddScoped<INotificationService, NotificationService>();

        if (!disableBackgroundJobs)
        {
            // Hangfire background jobs
            services.AddHangfire(cfg => cfg
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    configuration.GetConnectionString("DefaultConnection"),
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    }));

            services.AddHangfireServer(opts =>
            {
                opts.WorkerCount = 2;
                opts.Queues = ["default"];
            });

            // Jobs as transient so Hangfire can resolve them
            services.AddTransient<ContractExpiryJob>();
            services.AddTransient<SlaEscalationJob>();
        }

        // SignalR
        services.AddSignalR();

        return services;
    }
}
