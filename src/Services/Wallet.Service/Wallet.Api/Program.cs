using Microsoft.EntityFrameworkCore;
using Tessera.AspNetCore;
using Tessera.Security;
using Wallet.Api.Endpoints;
using Wallet.Application;
using Wallet.Infrastructure;
using Wallet.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTesseraWebDefaults("Tessera Wallet API");
builder.Services.AddTesseraJwtAuthentication(builder.Configuration);
builder.Services.AddWalletApplication();
builder.Services.AddWalletInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<WalletDbContext>("wallet-db");

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

app.MapAuthEndpoints();
app.MapWalletEndpoints();
app.MapHealthChecks("/health");

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<WalletDbContext>().Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program;
