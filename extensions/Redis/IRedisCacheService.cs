using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace SimpleCDN.Extensions.Redis
{
	/// <summary>
	/// Represents a service for storing and retrieving data in a Redis cache.
	/// </summary>
	/// <seealso cref="IDistributedCache"/>
	public interface IRedisCacheService : IDistributedCache
	{
		/// <summary>
		/// Gets the <see cref="ConnectionMultiplexer"/> instance used for the Redis cache.
		/// </summary>
		/// <seealso cref="ConnectionMultiplexer"/>
		/// <seealso cref="GetRedisConnectionAsync"/>
		public ConnectionMultiplexer GetRedisConnection();

		/// <summary>
		/// Gets the <see cref="ConnectionMultiplexer"/> instance used for the Redis cache asynchronously.
		/// </summary>
		/// <seealso cref="ConnectionMultiplexer"/>
		/// <seealso cref="GetRedisConnection"/>
		public Task<ConnectionMultiplexer> GetRedisConnectionAsync();
	}
}
