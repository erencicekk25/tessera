using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Tessera.AspNetCore;

public static class WebServiceExtensions
{
    /// <summary>Problem details, global exception handling and a JWT-aware Swagger document.</summary>
    public static IServiceCollection AddTesseraWebDefaults(this IServiceCollection services, string apiTitle)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = ctx =>
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHealthChecks();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = apiTitle, Version = "v1" });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            options.AddSecurityDefinition("Bearer", scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
        });

        return services;
    }
}
