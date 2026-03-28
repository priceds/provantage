using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProVantage.Domain.Interfaces;
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
        // EF Core + SQL Server
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();

            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                })
                .AddInterceptors(auditInterceptor, softDeleteInterceptor);
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "ProVantage:";
        });

        // Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<DatabaseSeeder>();

        // SignalR
        services.AddSignalR();

        return services;
    }
}
