using MediatR;
using Microsoft.Extensions.Logging;
using Tessera.Application.Behaviors;
using Tessera.Application.Messaging;
using Tessera.SharedKernel.Results;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Behaviors;

/// <summary>
/// Cache-aside for queries marked with <see cref="ICachedQuery"/>. Only the payload of successful
/// results is cached; write handlers evict the affected keys after commit.
/// </summary>
internal sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cache,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!typeof(TResponse).IsGenericType)
        {
            return await next();
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];

        try
        {
            var cached = await cache.GetAsync(request.CacheKey, valueType, cancellationToken);
            if (cached is not null)
            {
                logger.LogDebug("Cache hit for {CacheKey}", request.CacheKey);
                return ResultFactory<TResponse>.Success(cached);
            }
        }
        catch (Exception ex)
        {
            // A cache outage must degrade latency, never availability.
            logger.LogWarning(ex, "Cache read failed for {CacheKey}; falling back to the database", request.CacheKey);
        }

        var response = await next();

        if (response.IsSuccess)
        {
            try
            {
                var value = typeof(TResponse).GetProperty(nameof(Result<object>.Value))!.GetValue(response)!;
                await cache.SetAsync(request.CacheKey, value, valueType, request.Expiration, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cache write failed for {CacheKey}", request.CacheKey);
            }
        }

        return response;
    }
}
