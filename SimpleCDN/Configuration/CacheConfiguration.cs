namespace SimpleCDN.Configuration
{
	public class InMemoryCacheConfiguration
	{
		/// <summary>
		/// The maximum size of the in-memory cache in kB. Default is 500,000 (500 MB)
		/// </summary>
		public uint MaxSize { get; set; } = 500_000; // 500 MB
		/// <summary>
		/// The maximum age of an item in the cache in Minutes. Default is 1 day.
		/// Set to 0 to disable automatic expiration.
		/// </summary>
		public uint MaxAge { get; set; } = 60 * 24; // 1 day
		/// <summary>
		/// The interval at which the cache is purged of expired items in Minutes. Default is 1 hour.
		/// Set to 0 to disable automatic purging.
		/// </summary>
		public uint PurgeInterval { get; set; } = 60; // 1 hour
	}

	public class RedisCacheConfiguration
	{
		public string ConnectionString { get; set; } = "localhost";
		public string InstanceName { get; set; } = "SimpleCDN";
	}

	public class CacheConfiguration
	{
		/// <summary>
		/// The configuration for the in-memory cache
		/// </summary>
		public InMemoryCacheConfiguration? InMemory { get; set; }
		public RedisCacheConfiguration? Redis { get; set; }
		private int _type = -1;
		/// <summary>
		/// The type of cache to use. Defaults to Redis if configured, otherwise InMemory.
		/// Set to <see cref="CacheType.Disabled"/> to disable caching. This is useful for testing, but discouraged in production.
		/// <br/>
		/// Changing this value requires a restart of the application.
		/// </summary>
		public CacheType Type
		{
			get => Enum.IsDefined((CacheType)_type) ? (CacheType)_type : Redis is not null ? CacheType.Redis : CacheType.InMemory;
			set => _type = (int)value;
		}

		public enum CacheType
		{
			Disabled,
			InMemory,
			Redis
		}
	}
}
