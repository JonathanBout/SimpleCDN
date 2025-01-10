using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SimpleCDN.Services.Caching
{
	public class CacheManager(IDistributedCache cache, IOptionsMonitor<CacheConfiguration> options, ILogger<CacheManager> logger) : ICacheManager
	{
		private readonly IDistributedCache _cache = cache;
		private readonly IOptionsMonitor<CacheConfiguration> _options = options;
		private readonly ILogger<CacheManager> _logger = logger;
#if DEBUG
		private readonly List<ushort> _durations = new(100);
#endif
		public bool TryGetValue(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			var start = Stopwatch.GetTimestamp();
			var bytes = _cache.Get(key);
			TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
#if DEBUG
			if (_durations.Count == int.MaxValue - 10)
			{
				// remove about half of the durations to make room for new ones.
				// this has the nice side effect of newer values being more important
				_durations.RemoveAll(_ => Random.Shared.NextDouble() > .5);
			}
			_durations.Add((ushort)elapsed.TotalMilliseconds);
#endif
			if (bytes is null || bytes.Length == 0)
			{
				_logger.LogDebug("Cache MISS for {Key} in {Duration:0} ms", key, elapsed.TotalMilliseconds);
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
			_cache.SetAsync(path, file.GetBytes(), itemConfig);
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
			_cache.Remove(key);
			return true;
		}
		public bool TryRemove(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			if (TryGetValue(key, out value))
				return TryRemove(key);
			return false;
		}

		public object GetDebugView()
		{
#if DEBUG
			if (_cache is ICacheDebugInfoProvider debugInfoProvider)
			{
				return new DebugView(
					debugInfoProvider.GetType().Name,
					[.. _durations],
					debugInfoProvider.GetDebugInfo()
				);
			}

			return new DetailedDebugView(_cache.GetType().Name, _durations);
#else
			return new BasicDebugView(_cache.GetType().Name);
#endif
		}
	}

	internal class BasicDebugView(string implementation)
	{
		public string Implementation { get; } = implementation;
	}
#if DEBUG
	internal class DetailedDebugView(string implementation, List<ushort> durations) : BasicDebugView(implementation)
	{
		public double AverageDuration { get; } = durations.Average(v => (double)v);
		public double MaxDuration { get; } = durations.Max();
		public double MinDuration { get; } = durations.Min();

		public double Percentile50 { get; } = Percentile(durations, 0.5);
		public double Percentile90 { get; } = Percentile(durations, 0.9);
		public double Percentile95 { get; } = Percentile(durations, 0.95);
		public double Percentile99 { get; } = Percentile(durations, 0.99);

		public int TotalDurations { get; } = durations.Count;

		/// <summary>
		/// Calculates the percentile of the given durations.
		/// </summary>
		private static double Percentile(List<ushort> durations, double percentile)
		{
			var sorted = durations.Order().ToArray();
			int index = (int)(percentile * sorted.Length);
			return sorted[index];
		}
	}

	internal class DebugView(string implementation, List<ushort> durations, object details) : DetailedDebugView(implementation, durations)
	{
		public object Details { get; } = details;
	}
#endif
}
