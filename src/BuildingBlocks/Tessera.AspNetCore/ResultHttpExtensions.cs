using Microsoft.AspNetCore.Http;
using Tessera.SharedKernel.Results;

namespace Tessera.AspNetCore;

/// <summary>Maps application <see cref="Result"/> values to RFC 9457 problem responses.</summary>
public static class ResultHttpExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value)
            : result.Error.ToProblem();

    public static IResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error.Details is not null)
        {
            return Results.ValidationProblem(
                error.Details.ToDictionary(kv => kv.Key, kv => kv.Value),
                detail: error.Message,
                title: "Validation failed",
                extensions: extensions);
        }

        return Results.Problem(
            statusCode: status,
            title: Title(error.Type),
            detail: error.Message,
            extensions: extensions);
    }

    private static string Title(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.BusinessRule => "Business rule violated",
        _ => "Unexpected error"
    };
}
