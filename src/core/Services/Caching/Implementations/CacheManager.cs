using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Services.Caching.Implementations
{
	internal class CacheManager(ICacheImplementationResolver cacheResolver, IOptionsMonitor<CacheConfiguration> options, ILogger<CacheManager> logger) : ICacheManager
	{
		private IDistributedCache Cache => cacheResolver.Implementation;
		private readonly IOptionsMonitor<CacheConfiguration> _options = options;
		private readonly ILogger<CacheManager> _logger = logger;
#if DEBUG
#if NET9_0_OR_GREATER
		private readonly Lock _durationsLock = new();
#else
		private readonly object _durationsLock = new();
#endif
		private readonly List<ushort> _durations = new(100);
		private ulong _hitCount = 0;
		private ulong _missCount = 0;
#endif
		public bool TryGetValue(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			var start = Stopwatch.GetTimestamp();

			byte[]? bytes;

			try
			{
				bytes = Cache.Get(key);
			} catch (Exception ex)
			{
				_logger.LogError(ex, "Error while trying to get {Key} from cache", key);
#if DEBUG
				_missCount++;
#endif
				value = null;
				return false;
			}

			TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
#if DEBUG
			lock (_durationsLock)
			{
				if (_durations.Count == int.MaxValue - 10)
				{
					// remove about half of the durations to make room for new ones.
					// this has the nice side effect of newer values being more important
					_durations.RemoveAll(_ => Random.Shared.NextDouble() > .5);
				}

				_durations.Add((ushort)elapsed.TotalMilliseconds);
			}

			// increment hit or miss count
			Interlocked.Increment(ref bytes is not { Length: > 0 } ? ref _missCount : ref _hitCount);
#endif
			if (bytes is null || bytes.Length == 0)
			{
				_logger.LogInformation("Cache MISS for {Key} in {Duration:0} ms", key, elapsed.TotalMilliseconds);
				value = null;
				return false;
			}

			_logger.LogDebug("Cache HIT for {Key} in {Duration:0} ms", key, elapsed.TotalMilliseconds);

			value = CachedFile.FromBytes(bytes);

			return value is not null;
		}

		public void CacheFile(string path, CachedFile file)
		{
			// this config is not used by the local in-memory cache provider (it takes the MaxAge directly from the configuration),
			// but for example Redis does use it
			var itemConfig = new DistributedCacheEntryOptions
			{
				// Use sliding expiration if max age is set.
				// Because cached file is automatically invalidated when the file is updated, we can do this safely
				SlidingExpiration = _options.CurrentValue.MaxAge > 0 ? TimeSpan.FromMinutes(_options.CurrentValue.MaxAge) : null,
			};
			Cache.SetAsync(path, file.GetBytes(), itemConfig);
		}

		public void CacheFile(string path, byte[] content, int realSize, DateTimeOffset lastModified, MimeType mimeType, CompressionAlgorithm compression)
		{
			var file = new CachedFile
			{
				Content = content,
				LastModified = lastModified,
				MimeType = mimeType,
				Compression = compression,
				Size = content.Length
			};

			CacheFile(path, file);
		}

		public bool TryRemove(string key)
		{
			Cache.Remove(key);
			return true;
		}
		public bool TryRemove(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			if (TryGetValue(key, out value))
				return TryRemove(key);
			return false;
		}

#if DEBUG
		public object GetDebugView()
		{
			lock (_durationsLock)
			{
				if (Cache is ICacheDebugInfoProvider debugInfoProvider)
				{
					return new DebugView(
						debugInfoProvider.GetType().Name,
						[.. _durations],
						debugInfoProvider.GetDebugInfo(),
						_hitCount, _missCount
					);
				}

				return new DetailedDebugView(Cache.GetType().Name, _durations,
						_hitCount, _missCount);
			}
		}
#endif
	}

#if DEBUG
	internal class BasicDebugView(string implementation, ulong hitCount, ulong missCount)
	{
		public string Implementation { get; } = implementation;
		public ulong HitCount { get; } = hitCount;
		public ulong MissCount { get; } = missCount;
	}
	internal class DetailedDebugView : BasicDebugView
	{
		public double AverageDuration { get; }
		public double MaxDuration { get; }
		public double MinDuration { get; }

		public double Percentile50 { get; }
		public double Percentile90 { get; }
		public double Percentile95 { get; }
		public double Percentile99 { get; }

		public int TotalDurations { get; }

		public DetailedDebugView(string implementation,
						   List<ushort> durations,
						   ulong hitCount,
						   ulong missCount) : base(implementation, hitCount, missCount)
		{
			if (durations.Count > 0)
			{
				AverageDuration = durations.Average(v => (double)v);
				MaxDuration = durations.Max();
				MinDuration = durations.Min();
				Percentile50 = Percentile(durations, 0.5);
				Percentile90 = Percentile(durations, 0.9);
				Percentile95 = Percentile(durations, 0.95);
				Percentile99 = Percentile(durations, 0.99);
			}
			TotalDurations = durations.Count;
		}

		/// <summary>
		/// Calculates the percentile of the given durations.
		/// </summary>
		private static double Percentile(List<ushort> durations, double percentile)
		{
			var sorted = durations.Order().ToArray();
			var index = (int)(percentile * sorted.Length);
			return sorted[index];
		}
	}

	internal class DebugView(string implementation,
						  List<ushort> durations,
						  object details,
						  ulong hitCount,
						  ulong missCount) : DetailedDebugView(implementation, durations, hitCount, missCount)
	{
		public object Details { get; } = details;
	}
#endif

	/// <summary>
	/// Resolves the selected cache implementation from the registered services.
	/// </summary>
	internal class CacheImplementationResolver(IServiceProvider services, Type implementationType) : ICacheImplementationResolver
	{
		private IDistributedCache? _impl;
		public IDistributedCache Implementation
		{
			get
			{
				_impl ??= services.GetServices<IDistributedCache>().FirstOrDefault(s => s.GetType() == implementationType);

				if (_impl is null)
					throw new InvalidOperationException($"The specified cache implementation ({implementationType.Name}) is not registered.");

				return _impl;
			}
		}
	}
}
