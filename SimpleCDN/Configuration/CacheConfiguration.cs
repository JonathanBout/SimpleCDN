namespace SimpleCDN.Configuration
{
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

		/// <summary>
		/// The maximum age of an item in the cache in Minutes. Default is 1 day.
		/// Set to 0 to disable automatic expiration.
		/// </summary>
		public uint MaxAge { get; set; } = 60 * 24; // 1 day

		public enum CacheType
		{
			Disabled,
			InMemory,
			Redis
		}
	}
}
