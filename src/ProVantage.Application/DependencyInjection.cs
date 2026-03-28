using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProVantage.Application.Common.Behaviors;
using System.Reflection;

namespace ProVantage.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR + pipeline behaviors (order matters)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        });

        // FluentValidation — auto-register all validators in the assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
