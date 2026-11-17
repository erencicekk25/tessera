using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tessera.Application.Behaviors;

namespace Scoring.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddScoringApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
