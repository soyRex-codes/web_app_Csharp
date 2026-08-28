using System.Security.Claims;
using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Features.Transfers;

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/transfers", Transfer)
            .WithTags("Transfers")
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> Transfer(
        TransferRequest request,
        AccountOperationsService operations,
        ClaimsPrincipal user,
        CancellationToken cancellationToken) =>
        (await operations.TransferAsync(request, user, cancellationToken)).ToHttpResult(TypedResults.Ok);
}
