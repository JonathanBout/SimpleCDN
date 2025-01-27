using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace SimpleCDN.Extensions.Redis
{
	public interface IRedisCacheService : IDistributedCache
	{
		public ConnectionMultiplexer GetRedisConnection();
		public Task<ConnectionMultiplexer> GetRedisConnectionAsync();
	}
}
