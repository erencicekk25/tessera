using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scoring.Application.Abstractions;
using Scoring.Domain.Abstractions;
using Scoring.Infrastructure.Messaging;
using Scoring.Infrastructure.Persistence;

namespace Scoring.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddScoringInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgres = configuration.GetConnectionString("ScoringDb")
                       ?? throw new InvalidOperationException("Connection string 'ScoringDb' is missing.");

        services.AddDbContext<ScoringDbContext>(options =>
            options.UseNpgsql(postgres, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ScoringDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ScoringDbContext>());
        services.AddScoped<ICreditProfileRepository, CreditProfileRepository>();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddEntityFrameworkOutbox<ScoringDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
            });

            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                // Retries resolve optimistic-concurrency clashes when two events for the same user race.
                endpoint.UseMessageRetry(retry => retry.Intervals(100, 250, 500, 1000, 2000));
                endpoint.UseEntityFrameworkOutbox<ScoringDbContext>(context);
            });

            bus.AddConsumer<UserRegisteredConsumer>();
            bus.AddConsumer<TransactionCompletedConsumer>();
            bus.AddConsumer<TransactionDeclinedConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = configuration.GetSection("RabbitMq");
                cfg.Host(rabbit["Host"] ?? "localhost", rabbit["VirtualHost"] ?? "/", host =>
                {
                    host.Username(rabbit["Username"] ?? "guest");
                    host.Password(rabbit["Password"] ?? "guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
