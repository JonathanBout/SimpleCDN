using Microsoft.Extensions.Caching.Distributed;
using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SimpleCDN.Services
{
	public class CacheManager(IDistributedCache cache) : ICacheManager
	{
		private readonly IDistributedCache _cache = cache;

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
			_cache.SetAsync(path, file.GetBytes());
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
			{
				return TryRemove(key);
			}
			return false;
		}

		public object GetDebugView()
		{
			if (_cache is SizeLimitedCache slc)
			{
				return new DebugView(
					slc.GetType().Name,
					slc.Size,
					slc.Count,
					[.. slc.Keys]
				);
			}

			return new BasicDebugView(_cache.GetType().Name);
		}
	}

	internal record BasicDebugView(string Implementation);

	internal record DebugView(string Implementation, long Size, int Count, string[] Elements) : BasicDebugView(Implementation);
}
