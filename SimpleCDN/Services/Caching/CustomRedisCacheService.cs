using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using StackExchange.Redis;
using System.Diagnostics;

namespace SimpleCDN.Services.Caching
{
	public sealed class CustomRedisCacheService : IDistributedCache, IAsyncDisposable, ICacheDebugInfoProvider
	{
		private readonly IOptionsMonitor<CacheConfiguration> _cacheConfig;
		private readonly ObjectAccessBalancer<ConnectionMultiplexer> _redisConnectionMultiplexers;
		private readonly ILogger<CustomRedisCacheService> _logger;

		private IDatabase Database => _redisConnectionMultiplexers.Next().GetDatabase();
		private TimeSpan MaxAge => TimeSpan.FromMinutes(_cacheConfig.CurrentValue.MaxAge);

		DateTimeOffset lastTimeout = DateTimeOffset.MaxValue;
		ushort multiplexerTargetCount = 1;

		public CustomRedisCacheService(IOptionsMonitor<CacheConfiguration> cacheConfig, ILogger<CustomRedisCacheService> logger)
		{
			_cacheConfig = cacheConfig;
			_logger = logger;
			if (_cacheConfig.CurrentValue is { Type: not CacheConfiguration.CacheType.Redis } or { Redis: null })
			{
				throw new InvalidOperationException("Redis cache service is registered, but redis is not configured.");
			}

			// create the instance balancer with the instance factory
			_redisConnectionMultiplexers = new(
				() => ConnectionMultiplexer.Connect(
							_cacheConfig.CurrentValue.Redis!.ConnectionString,
							options => options.ClientName = cacheConfig.CurrentValue.Redis!.InstanceName));
		}

		public ValueTask DisposeAsync() => _redisConnectionMultiplexers.DisposeAsync();

		public byte[]? Get(string key)
		{
			return Get(key, _redisConnectionMultiplexers.Next());
		}

		private byte[]? Get(string key, ConnectionMultiplexer multiplexer, int retryCount = 0)
		{
			const int maxRetries = 3;
			if (retryCount > maxRetries)
			{
				return null;
			}

			try
			{
				// keep track of how long the operation takes,

				var start = Stopwatch.GetTimestamp();
				IDatabase database = multiplexer.GetDatabase();

				RedisValue result = database.StringGet(key);
				if (result.HasValue)
				{
					// reset the expiration time to have a sliding expiration
					database.KeyExpire(key, MaxAge);
				}
				TimeSpan elapsed = Stopwatch.GetElapsedTime(start);

				// if the operation took more than 100ms, create a new multiplexer
				if (elapsed.TotalMilliseconds > 100)
				{
					CreateNewMultiplexer();
				} else
				{
					// no timeout occurred, check if we can reduce the number of multiplexers
					SyncMultiplexers();
				}

				return result;
			} catch (RedisTimeoutException)
			{
				// timeout occured, create a new multiplexer and use that to try again, up to 3 times
				_logger.LogError("Redis timeout exception occurred. Trying to reconnect ({count}/{maxRetries}).", retryCount, maxRetries);

				return Get(key, CreateNewMultiplexer(), retryCount + 1);
			}
		}

		/// <summary>
		/// Create a new multiplexer and set the last timeout to now.
		/// </summary>
		private ConnectionMultiplexer CreateNewMultiplexer()
		{
			lastTimeout = DateTimeOffset.UtcNow;
			return _redisConnectionMultiplexers.AddInstance();
		}

		public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
				return Task.FromCanceled<byte[]?>(token);
			return Task.FromResult(Get(key));
		}

		public void Refresh(string key) { }
		public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
		public void Remove(string key) => Database.StringGetDelete(key);
		public Task RemoveAsync(string key, CancellationToken token = default) => Database.StringGetDeleteAsync(key);

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			Database.StringSet(key, value, MaxAge, flags: CommandFlags.FireAndForget);
		}

		public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			await Database.StringSetAsync(key, value, MaxAge, flags: CommandFlags.FireAndForget);
		}

		private readonly SemaphoreSlim _syncLock = new(1, 1);
		/// <summary>
		/// If no timeouts have occurred in the last 15 seconds, reduce the number of multiplexers by 1.
		/// </summary>
		private void SyncMultiplexers()
		{
			if (_syncLock.CurrentCount == 0)
			{
				return;
			}
			_syncLock.Wait();
			Task.Run(() =>
			{
				if (DateTimeOffset.Now - lastTimeout > TimeSpan.FromSeconds(15))
				{
					multiplexerTargetCount--;
					if (multiplexerTargetCount < 1)
					{
						multiplexerTargetCount = 1;
					}
				}
				while (_redisConnectionMultiplexers.Count > multiplexerTargetCount)
				{
					_redisConnectionMultiplexers.RemoveOneInstance();
				}
			})
				// release the lock when the task is done.
				// ContinueWith is equivalent to finally in this case.
				.ContinueWith(_ => _syncLock.Release());
		}

		/// <summary>
		/// Get an object containing information about the multiplexers and the last timeout.
		/// </summary>
		public object GetDebugInfo()
		{
			return new CustomRedisCacheServiceDebugView(_redisConnectionMultiplexers.Count, multiplexerTargetCount, _redisConnectionMultiplexers.HighestCount, lastTimeout == DateTimeOffset.MaxValue ? "never" : lastTimeout.UtcDateTime.ToString());
		}
	}

	public record CustomRedisCacheServiceDebugView(int MultiplexerCount, int TargetMultiplexerCount, int HighestMultiplexerCount, string LastTimeout);
}
