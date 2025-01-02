using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using SimpleCDN.Services;
using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Tests.Mocks
{
	public class MockCacheManager : ICacheManager
	{
		public void CacheFile(string path, byte[] content, int realSize, DateTimeOffset lastModified, MimeType mimeType, CompressionAlgorithm compression) { }
		public void CacheFile(string path, CachedFile file) { }
		public bool TryGetValue(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			value = null;
			return false;
		}
		public bool TryRemove(string key) => true;
		public bool TryRemove(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			value = null;
			return false;
		}
	}
}
