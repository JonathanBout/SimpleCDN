namespace SimpleCDN.Configuration
{
	/// <summary>
	/// Represents the configuration for the in-memory cache used by SimpleCDN.
	/// </summary>
	public class InMemoryCacheConfiguration
	{
		/// <summary>
		/// The maximum size of the in-memory cache in kB. Default is 500,000 (500 MB).
		/// </summary>
		public uint MaxSize { get; set; } = 500_000; // 500 MB

		/// <summary>
		/// This value is not used anymore, and will be removed in a future version.
		/// The cache doesn't work with purging intervals anymore, and instead directly waits for the oldest item to expire.
		/// <br/> <br/>
		/// The interval at which the cache is purged of expired items in Minutes. Default is 5 minutes.
		/// Set to 0 to disable automatic purging.
		/// </summary>
		[Obsolete("The cache doesn't work with purging intervals anymore. This value is not used anymore, and will be removed in a future version.")]
		public TimeSpan PurgeInterval { get; set; } = TimeSpan.FromMinutes(5); // 5 minutes
	}
}
