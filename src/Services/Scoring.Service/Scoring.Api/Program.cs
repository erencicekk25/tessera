using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Scoring.Api.Realtime;
using Scoring.Application;
using Scoring.Application.Abstractions;
using Scoring.Application.Features.CreditReports;
using Scoring.Infrastructure;
using Scoring.Infrastructure.Persistence;
using Tessera.AspNetCore;
using Tessera.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTesseraWebDefaults("Tessera Scoring API");
builder.Services.AddTesseraJwtAuthentication(builder.Configuration, hubPathPrefix: ScoreHub.Route);
builder.Services.AddScoringApplication();
builder.Services.AddScoringInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<ScoringDbContext>("scoring-db");

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
builder.Services.AddSingleton<IScoreNotifier, SignalRScoreNotifier>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/scores/me", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetCreditReportQuery(user.GetRequiredUserId(), user.Identity?.Name), ct)).ToHttpResult())
    .RequireAuthorization()
    .WithTags("Credit score")
    .WithName("GetMyCreditReport")
    .Produces<CreditReportDto>();

app.MapHub<ScoreHub>(ScoreHub.Route);
app.MapHealthChecks("/health");

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ScoringDbContext>().Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program;
