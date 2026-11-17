using System.Collections.Concurrent;
using System.Reflection;
using FluentValidation;
using MediatR;
using Tessera.SharedKernel.Results;

namespace Tessera.Application.Behaviors;

/// <summary>
/// Runs every FluentValidation validator for the request and short-circuits with a
/// <see cref="ErrorType.Validation"/> result, so handlers only ever see well-formed input.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count == 0)
        {
            return await next();
        }

        var details = failures
            .GroupBy(f => ToCamelCase(f.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        var error = Error.Validation("Request.Invalid", "One or more fields are invalid.") with { Details = details };
        return ResultFactory<TResponse>.Failure(error);
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}

/// <summary>Builds a failed <c>Result</c> or <c>Result&lt;T&gt;</c> without knowing <typeparamref name="TResponse"/> statically.</summary>
public static class ResultFactory<TResponse>
    where TResponse : Result
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> GenericFailures = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> GenericSuccesses = new();

    public static TResponse Success(object value)
    {
        var method = GenericSuccesses.GetOrAdd(typeof(TResponse), t =>
            typeof(Result).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m is { Name: nameof(Result.Success), IsGenericMethodDefinition: true })
                .MakeGenericMethod(t.GetGenericArguments()[0]));

        return (TResponse)method.Invoke(null, [value])!;
    }

    public static TResponse Failure(Error error)
    {
        var type = typeof(TResponse);
        if (type == typeof(Result))
        {
            return (TResponse)Result.Failure(error);
        }

        var method = GenericFailures.GetOrAdd(type, t =>
            typeof(Result).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true })
                .MakeGenericMethod(t.GetGenericArguments()[0]));

        return (TResponse)method.Invoke(null, [error])!;
    }
}
