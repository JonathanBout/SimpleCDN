using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using StackExchange.Redis;

namespace SimpleCDN.Services.Caching
{
	public sealed class CustomRedisCacheService : IDistributedCache, IDisposable
	{
		private readonly IOptionsMonitor<CacheConfiguration> _cacheConfig;
		private readonly ConnectionMultiplexer _redisConnectionMultiplexer;

		private IDatabase Database => _redisConnectionMultiplexer.GetDatabase();
		private TimeSpan MaxAge => TimeSpan.FromMinutes(_cacheConfig.CurrentValue.MaxAge);

		public CustomRedisCacheService(IOptionsMonitor<CacheConfiguration> cacheConfig)
		{
			_cacheConfig = cacheConfig;
			if (_cacheConfig.CurrentValue is { Type: not CacheConfiguration.CacheType.Redis } or { Redis: null })
			{
				throw new InvalidOperationException("Redis cache service is registered, but redis is not configured.");
			}

			_redisConnectionMultiplexer = ConnectionMultiplexer.Connect(_cacheConfig.CurrentValue.Redis.ConnectionString);
		}

		public void Dispose()
		{
			_redisConnectionMultiplexer.Dispose();
		}

		public byte[]? Get(string key)
		{
			RedisValue result = Database.StringGet(key);
			Database.KeyExpire(key, MaxAge);
			return result;
		}

		public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
		{
			RedisValue result = await Database.StringGetAsync(key);
			await Database.KeyExpireAsync(key, MaxAge);
			return result;
		}

		public void Refresh(string key) { }
		public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
		public void Remove(string key) => Database.StringGetDelete(key);
		public Task RemoveAsync(string key, CancellationToken token = default) => Database.StringGetDeleteAsync(key);
		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			Database.StringSet(key, value, MaxAge);
		}

		public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			await Database.StringSetAsync(key, value, MaxAge);
		}
	}
}
