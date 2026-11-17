using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Wallet.Application.Abstractions;

namespace Wallet.Infrastructure.Caching;

internal sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public async Task<object?> GetAsync(string key, Type valueType, CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        return bytes is null ? null : JsonSerializer.Deserialize(bytes, valueType, Json);
    }

    public Task SetAsync(string key, object value, Type valueType, TimeSpan? expiration = null, CancellationToken cancellationToken = default) =>
        cache.SetAsync(
            key,
            JsonSerializer.SerializeToUtf8Bytes(value, valueType, Json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration },
            cancellationToken);

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);
}
