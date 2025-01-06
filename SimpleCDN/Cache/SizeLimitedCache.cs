using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;

namespace SimpleCDN.Cache
{
	/// <summary>
	/// A local, in-memory cache that limits the total size of the stored values. When the size of the cache exceeds the specified limit, the oldest (least recently accessed) values are removed.<br/>
	/// Implements <see cref="IDistributedCache"/> for compatibility with the <see cref="Services.CacheManager"/>. It is not actually distributed.
	/// </summary>
	internal class SizeLimitedCache(IOptionsMonitor<InMemoryCacheConfiguration> options, IOptionsMonitor<CacheConfiguration> cacheOptions, ILogger<SizeLimitedCache> logger) : IDistributedCache, IHostedService
	{
		private readonly IOptionsMonitor<InMemoryCacheConfiguration> _options = options;
		private readonly IOptionsMonitor<CacheConfiguration> _cacheOptions = cacheOptions;
		private readonly ConcurrentDictionary<string, ValueWrapper> _dictionary = new(StringComparer.OrdinalIgnoreCase);
		public long MaxSize => _options.CurrentValue.MaxSize * 1000; // convert from kB to B
		private readonly ILogger<SizeLimitedCache> _logger = logger;
		public long Size => _dictionary.Values.Sum(wrapper => (long)wrapper.Size);

		public int Count => _dictionary.Count;

		public IEnumerable<string> Keys => _dictionary.OrderBy(kvp => kvp.Value.AccessedAt).Select(kvp => kvp.Key);

		public byte[]? Get(string key)
		{
			if (_dictionary.TryGetValue(key, out ValueWrapper? wrapper))
			{
				_logger.LogDebug("Cache HIT {key}", key.ForLog());
				return wrapper.Value;
			}

			_logger.LogDebug("Cache MISS {key}", key.ForLog());

			return null;
		}

		public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
		{
			return token.IsCancellationRequested ? Task.FromCanceled<byte[]?>(token) : Task.FromResult(Get(key));
		}

		public void Refresh(string key) { }
		public Task RefreshAsync(string key, CancellationToken token = default) => token.IsCancellationRequested ? Task.FromCanceled(token) : Task.CompletedTask;
		public void Remove(string key) => _dictionary.TryRemove(key, out _);
		public Task RemoveAsync(string key, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
			{
				return Task.FromCanceled(token);
			}

			Remove(key);
			return Task.CompletedTask;
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			_dictionary[key] = new ValueWrapper(value);

			IEnumerable<KeyValuePair<string, ValueWrapper>> byOldest = _dictionary.OrderBy(wrapper => wrapper.Value.AccessedAt).AsEnumerable();

			// remove the oldest items until the size is within the limit
			while (Size > MaxSize)
			{
				try
				{
					((string oldestKey, _), byOldest) = byOldest.RemoveFirst();
					_dictionary.TryRemove(oldestKey, out _);
				} catch (ArgumentOutOfRangeException)
				{
					// code should never reach this point, but just in case as a safety net
					break;
				}
			}
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
			{
				return Task.FromCanceled(token);
			}

			Set(key, value, options);
			return Task.CompletedTask;
		}

		#region Automatic Purging
		private void Purge()
		{
			TimeSpan maxAge = TimeSpan.FromMinutes(_cacheOptions.CurrentValue.MaxAge);
			_dictionary.RemoveWhere(kvp => Stopwatch.GetElapsedTime(kvp.Value.AccessedAt) < maxAge);
		}

		private async Task PurgeLoop()
		{
			while (_backgroundCTS?.Token.IsCancellationRequested is false)
			{
				await Task.Delay(TimeSpan.FromMinutes(_options.CurrentValue.PurgeInterval), _backgroundCTS.Token);

				if (_backgroundCTS.Token.IsCancellationRequested)
				{
					break;
				}

				_logger.LogDebug("Purging expired cache items");

				Purge();
			}
		}

		private CancellationTokenSource? _backgroundCTS;
		private IDisposable? _optionsOnChange;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			// register the options change event to restart the background task
			_optionsOnChange ??= _options.OnChange(_ => StartAsync(default));

			if (_cacheOptions.CurrentValue.MaxAge == 0 || _options.CurrentValue.PurgeInterval == 0)
			{
				// automatic expiration and purging are disabled,
				// stop the background task if it's running and return
				_backgroundCTS?.Dispose();
				_backgroundCTS = null;
				return Task.CompletedTask;
			}

			if (_backgroundCTS is not null)
			{
				// background task is already running, no need to start another
				return Task.CompletedTask;
			}
			_backgroundCTS = new CancellationTokenSource();

			_backgroundCTS.Token.Register(() =>
			{
				// if the token is cancelled, dispose the token source and set it to null
				// so that the next time StartAsync is called it may be recreated
				_backgroundCTS?.Dispose();
				_backgroundCTS = null;
			});

			// The background task will run in the background until the token is cancelled
			// execution of the current method will continue immediately
			Task.Run(PurgeLoop,
				CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _backgroundCTS.Token).Token);

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return _backgroundCTS?.CancelAsync() ?? Task.CompletedTask;
		}

		#endregion

		class ValueWrapper(byte[] value)
		{
			private byte[] _value = value;
			public byte[] Value
			{
				get
				{
					AccessedAt = Stopwatch.GetTimestamp();
					return _value;
				}
				set
				{
					_value = value;
					AccessedAt = Stopwatch.GetTimestamp();
				}
			}
			public int Size => _value.Length;
			public long AccessedAt { get; private set; } = Stopwatch.GetTimestamp();

			public static implicit operator byte[](ValueWrapper wrapper) => wrapper.Value;
		}
	}
}
