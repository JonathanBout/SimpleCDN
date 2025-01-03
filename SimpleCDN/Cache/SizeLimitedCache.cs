﻿using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SimpleCDN.Cache
{
	/// <summary>
	/// A local, in-memory cache that limits the total size of the stored values. When the size of the cache exceeds the specified limit, the oldest (least recently accessed) values are removed.<br/>
	/// Implements <see cref="IDistributedCache"/> for compatibility with the <see cref="Services.CacheManager"/>. It is not actually distributed.
	/// </summary>
	internal class SizeLimitedCache(IOptionsMonitor<InMemoryCacheConfiguration> options, ILogger<SizeLimitedCache> logger) : IDistributedCache
	{
		private readonly ConcurrentDictionary<string, ValueWrapper> _dictionary = new(StringComparer.OrdinalIgnoreCase);
		private long MaxSize => options.CurrentValue.MaxSize * 1000; // convert from kB to B
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
