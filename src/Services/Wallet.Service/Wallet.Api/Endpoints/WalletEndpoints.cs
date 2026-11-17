using System.Security.Claims;
using MediatR;
using Tessera.AspNetCore;
using Tessera.Security;
using Wallet.Application.Features.Wallets;

namespace Wallet.Api.Endpoints;

public static class WalletEndpoints
{
    public sealed record MoneyMovementRequest(decimal Amount, string? Description);

    public sealed record TransferRequest(string RecipientEmail, decimal Amount, string? Note);

    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wallet").WithTags("Wallet").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetWalletSummaryQuery(user.GetRequiredUserId()), ct)).ToHttpResult())
            .WithName("GetWalletSummary")
            .Produces<WalletSummaryDto>();

        group.MapGet("/ledger", async (ClaimsPrincipal user, ISender sender, CancellationToken ct, int page = 1, int pageSize = 10) =>
                (await sender.Send(new GetLedgerQuery(user.GetRequiredUserId(), page, pageSize), ct)).ToHttpResult())
            .WithName("GetLedger")
            .Produces<PagedResult<LedgerEntryDto>>();

        group.MapPost("/deposits", async (MoneyMovementRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DepositFundsCommand(user.GetRequiredUserId(), request.Amount, request.Description), ct))
                .ToHttpResult(receipt => Results.Created($"/api/wallet/ledger/{receipt.EntryId}", receipt)))
            .WithName("Deposit")
            .Produces<TransactionReceiptDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPost("/withdrawals", async (MoneyMovementRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
                (await sender.Send(new WithdrawFundsCommand(user.GetRequiredUserId(), request.Amount, request.Description), ct))
                .ToHttpResult(receipt => Results.Created($"/api/wallet/ledger/{receipt.EntryId}", receipt)))
            .WithName("Withdraw")
            .Produces<TransactionReceiptDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/transfers", async (TransferRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
                (await sender.Send(new TransferFundsCommand(user.GetRequiredUserId(), request.RecipientEmail, request.Amount, request.Note), ct))
                .ToHttpResult(receipt => Results.Created($"/api/wallet/ledger/{receipt.EntryId}", receipt)))
            .WithName("Transfer")
            .Produces<TransactionReceiptDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}
