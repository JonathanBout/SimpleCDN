using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SimpleCDN.Services.Caching
{
	public class CacheManager(IDistributedCache cache, IOptionsMonitor<CacheConfiguration> options) : ICacheManager
	{
		private readonly IDistributedCache _cache = cache;
		private readonly IOptionsMonitor<CacheConfiguration> _options = options;

		public bool TryGetValue(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			var bytes = _cache.Get(key);

			if (bytes is null || bytes.Length == 0)
			{
				value = null;
				return false;
			}

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
			if (_cache is ICacheDebugInfoProvider debugInfoProvider)
			{
				return new DebugView(
					debugInfoProvider.GetType().Name,
					debugInfoProvider.GetDebugInfo()
				);
			}

			return new BasicDebugView(_cache.GetType().Name);
		}
	}

	internal record BasicDebugView(string Implementation);
	internal record DebugView(string Implementation, object Details) : BasicDebugView(Implementation);
}
