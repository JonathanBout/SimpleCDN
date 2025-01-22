using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SimpleCDN.Extensions.Redis
{
	internal sealed class CustomRedisCacheService : IDistributedCache
	{
		private readonly IOptionsMonitor<RedisCacheConfiguration> _options;
		private readonly IOptionsMonitor<CacheConfiguration> _cacheOptions;

		private readonly ConnectionMultiplexer _redisConnection;

		public CustomRedisCacheService(IOptionsMonitor<RedisCacheConfiguration> options, IOptionsMonitor<CacheConfiguration> cacheOptions)
		{
			_options = options;
			_cacheOptions = cacheOptions;
			_redisConnection = ConnectionMultiplexer.Connect(_options.CurrentValue.ConnectionString,
				options => options.ClientName = _options.CurrentValue.ClientName);
		}

		public IDatabase Database => _redisConnection.GetDatabase();

		public byte[]? Get(string key)
		{
			return Database.StringGetSetExpiry(key, _cacheOptions.CurrentValue.MaxAge);
		}

		public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
		{
			return await Database.StringGetSetExpiryAsync(key, _cacheOptions.CurrentValue.MaxAge);
		}

		public void Refresh(string key) { }
		public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
		public void Remove(string key) => Database.StringGetDelete(key);
		public Task RemoveAsync(string key, CancellationToken token = default) => Database.StringGetDeleteAsync(key);

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			Database.StringSet(key, value, _cacheOptions.CurrentValue.MaxAge);
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			return Database.StringSetAsync(key, value, _cacheOptions.CurrentValue.MaxAge);
		}
	}
}
