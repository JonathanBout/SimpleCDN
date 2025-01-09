using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Services.Caching
{
	public interface ICacheManager
	{
		void CacheFile(string path, byte[] content, int realSize, DateTimeOffset lastModified, MimeType mimeType, CompressionAlgorithm compression);
		void CacheFile(string path, CachedFile file);
		bool TryGetValue(string key, [NotNullWhen(true)] out CachedFile? value);
		bool TryRemove(string key);
		bool TryRemove(string key, [NotNullWhen(true)] out CachedFile? value);

#if DEBUG
		object GetDebugView();
#endif
	}
}
