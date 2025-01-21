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
		private readonly IOptionsMonitor<RedisCacheConfiguration> options;
		private readonly IOptionsMonitor<CacheConfiguration> cacheOptions;

		private readonly ConnectionMultiplexer _redisConnection;

		public CustomRedisCacheService(IOptionsMonitor<RedisCacheConfiguration> options, IOptionsMonitor<CacheConfiguration> cacheOptions)
		{
			this.options = options;
			this.cacheOptions = cacheOptions;
			_redisConnection = ConnectionMultiplexer.Connect(Configuration.ConnectionString,
				config => config.ClientName = Configuration.ClientName);
		}

		private RedisCacheConfiguration Configuration => options.CurrentValue;
		private CacheConfiguration CacheConfiguration => cacheOptions.CurrentValue;

		private IDatabase Database => _redisConnection.GetDatabase().WithKeyPrefix(Configuration.KeyPrefix);

		public byte[]? Get(string key)
		{
			return Database.StringGetSetExpiry(key, TimeSpan.FromMinutes(CacheConfiguration.MaxAge));
		}

		public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
		{
			return await Database.StringGetSetExpiryAsync(key, TimeSpan.FromMinutes(CacheConfiguration.MaxAge));
		}

		public void Refresh(string key) { }

		public Task RefreshAsync(string key, CancellationToken token = default) => token.IsCancellationRequested ? Task.FromCanceled(token) : Task.CompletedTask;
		public void Remove(string key)
		{
			Database.KeyDelete(key);
		}

		public Task RemoveAsync(string key, CancellationToken token = default)
		{
			return Database.KeyDeleteAsync(key);
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			Database.StringSet(key, value, TimeSpan.FromMinutes(CacheConfiguration.MaxAge));
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			return Database.StringSetAsync(key, value, TimeSpan.FromMinutes(CacheConfiguration.MaxAge));
		}
	}
}
