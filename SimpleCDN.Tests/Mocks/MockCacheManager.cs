using SimpleCDN.Cache;
using SimpleCDN.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Tests.Mocks
{
	internal class MockCacheManager : ICacheManager
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
