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
		/// The type of cache to use. By default or when the value is invalid, uses Redis if it has been configured, otherwise InMemory.
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
		/// <para>
		/// The maximum age of an item in the cache in Minutes. Default is 1 day.
		/// </para>
		/// <para>
		/// This expiration is sliding, meaning that the expiration time is reset every time the item is accessed.
		/// Set to 0 to disable automatic expiration.
		/// </para>
		/// </summary>
		public uint MaxAge { get; set; } = 60 * 24; // 1 day

		/// <summary>
		/// The type of cache to use
		/// </summary>
		public enum CacheType
		{
			/// <summary>
			/// Disable caching
			/// </summary>
			Disabled,
			/// <summary>
			/// Store all cache locally, in-memory
			/// </summary>
			InMemory,
			/// <summary>
			/// Store all cache in a Redis server
			/// </summary>
			Redis
		}
	}
}
