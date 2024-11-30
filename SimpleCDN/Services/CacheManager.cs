using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Services
{
	public class CacheManager(IDistributedCache cache)
	{
		private readonly IDistributedCache _cache = cache;

		public bool TryGetValue(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			//if (_cache.SetAsync())
			//{
			//	value = CachedFile.FromBytes(data);
			//	return value is not null;
			//}
			value = default;
			return false;
		}
	}
}
