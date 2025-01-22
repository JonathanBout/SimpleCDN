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
	internal sealed class CustomRedisCacheService(IOptionsMonitor<RedisCacheConfiguration> options, IOptionsMonitor<CacheConfiguration> cacheOptions)
		: IDistributedCache, IAsyncDisposable, IDisposable
	{
		private readonly IOptionsMonitor<RedisCacheConfiguration> options = options;
		private readonly IOptionsMonitor<CacheConfiguration> cacheOptions = cacheOptions;

		private ConnectionMultiplexer? _redisConnection;
		private RedisCacheConfiguration Configuration => options.CurrentValue;
		private CacheConfiguration CacheConfiguration => cacheOptions.CurrentValue;

		private readonly SemaphoreSlim _redisConnectionLock = new(1);

		/// <summary>
		/// Checks if the Redis connection is still valid and creates a new one if necessary.
		/// </summary>
		private ConnectionMultiplexer GetRedisConnection()
		{
			_redisConnectionLock.Wait();

			try
			{
				if (_redisConnection is not { IsConnected: true } or { IsConnecting: true })
				{
					_redisConnection?.Dispose();
					_redisConnection = null;
				}

				return _redisConnection ??= ConnectionMultiplexer.Connect(
					Configuration.ConnectionString,
					config => config.ClientName = Configuration.ClientName
					);
			} finally
			{
				_redisConnectionLock.Release();
			}
		}
		private async Task<ConnectionMultiplexer> GetRedisConnectionAsync()
		{
			await _redisConnectionLock.WaitAsync();
			try
			{

				if (_redisConnection is not { IsConnected: true } or { IsConnecting: true })
				{
					_redisConnection?.Dispose();
					_redisConnection = null;
				}

				return _redisConnection ??= await ConnectionMultiplexer.ConnectAsync(
					Configuration.ConnectionString,
					config => config.ClientName = Configuration.ClientName
					);
			} finally
			{
				_redisConnectionLock.Release();
			}
		}

		private IDatabase GetDatabase() => GetRedisConnection().GetDatabase().WithKeyPrefix(Configuration.KeyPrefix);
		private async Task<IDatabaseAsync> GetDatabaseAsync() => (await GetRedisConnectionAsync()).GetDatabase().WithKeyPrefix(Configuration.KeyPrefix);

		public byte[]? Get(string key)
		{
			return GetDatabase().StringGetSetExpiry(key, CacheConfiguration.MaxAge);
		}

		public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
		{
			return await (await GetDatabaseAsync()).StringGetSetExpiryAsync(key, CacheConfiguration.MaxAge);
		}

		public void Refresh(string key) { }

		public Task RefreshAsync(string key, CancellationToken token = default) => token.IsCancellationRequested ? Task.FromCanceled(token) : Task.CompletedTask;
		public void Remove(string key)
		{
			GetDatabase().KeyDelete(key);
		}

		public async Task RemoveAsync(string key, CancellationToken token = default)
		{
			await (await GetDatabaseAsync()).KeyDeleteAsync(key);
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			GetDatabase().StringSet(key, value, CacheConfiguration.MaxAge);
		}

		public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			await (await GetDatabaseAsync()).StringSetAsync(key, value, CacheConfiguration.MaxAge);
		}

		public ValueTask DisposeAsync()
		{
			return _redisConnection?.DisposeAsync() ?? ValueTask.CompletedTask;
		}

		public void Dispose() => _redisConnection?.Dispose();
	}
}
