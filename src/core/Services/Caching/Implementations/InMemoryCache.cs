using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SimpleCDN.Services.Caching.Implementations
{
	/// <summary>
	/// A local, in-memory cache that limits the total size of the stored values. When the size of the cache exceeds the specified limit, the oldest (least recently accessed) values are removed.<br/>
	/// Implements <see cref="IDistributedCache"/> for compatibility with the <see cref="CacheManager"/>. It is not actually distributed.
	/// </summary>
	internal partial class InMemoryCache(
		IOptionsMonitor<InMemoryCacheConfiguration> options,
		IOptionsMonitor<CacheConfiguration> cacheOptions,
		ILogger<InMemoryCache> logger)
		: IDistributedCache, IDisposable
	{
		private readonly IOptionsMonitor<InMemoryCacheConfiguration> _options = options;
		private readonly IOptionsMonitor<CacheConfiguration> _cacheOptions = cacheOptions;
		private readonly ConcurrentDictionary<string, ValueWrapper> _dictionary = new(StringComparer.OrdinalIgnoreCase);
		public long MaxSize => _options.CurrentValue.MaxSize * 1000; // convert from kB to B
		private readonly ILogger<InMemoryCache> _logger = logger;

		public long Size => _dictionary.Values.Sum(wrapper => (long)wrapper.Size);

		public int Count => _dictionary.Count;

		public IEnumerable<string> Keys => _dictionary.OrderBy(kvp => kvp.Value.AccessedAt).Select(kvp => kvp.Key);

		public byte[]? Get(string key)
		{
			if (_dictionary.TryGetValue(key, out ValueWrapper? wrapper))
			{
				return wrapper.Value;
			}

			return null;
		}

		public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
		{
			return token.IsCancellationRequested ? Task.FromCanceled<byte[]?>(token) : Task.FromResult(Get(key));
		}

		public void Refresh(string key)
		{
			if (_dictionary.TryGetValue(key, out ValueWrapper? wrapper))
			{
				wrapper.Refresh();
			}
		}

		public Task RefreshAsync(string key, CancellationToken token = default)
			=> token.IsCancellationRequested ? Task.FromCanceled(token) : Task.CompletedTask;
		public void Remove(string key) => _dictionary.TryRemove(key, out _);
		public Task RemoveAsync(string key, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
				return Task.FromCanceled(token);

			Remove(key);
			return Task.CompletedTask;
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			_dictionary[key] = new ValueWrapper(value);
			Compact();
			Purge();
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			if (token.IsCancellationRequested)
				return Task.FromCanceled(token);

			Set(key, value, options);
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			DisposeCompacting();
			DisposePurging();
		}

		class ValueWrapper(byte[] value)
		{
			private byte[] _value = value;

			/// <summary>
			/// The value stored in the cache. Accessing this property will update <see cref="AccessedAt"/> to the current time.
			/// </summary>
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

			public void Refresh() => AccessedAt = Stopwatch.GetTimestamp();

			public int Size => _value.Length;
			public long AccessedAt { get; private set; } = Stopwatch.GetTimestamp();

			public static implicit operator byte[](ValueWrapper wrapper) => wrapper.Value;
		}
	}
}
