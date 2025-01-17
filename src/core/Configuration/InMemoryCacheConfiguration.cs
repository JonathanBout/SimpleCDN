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
		/// The interval at which the cache is purged of expired items in Minutes. Default is 5 minutes.
		/// Set to 0 to disable automatic purging.
		/// </summary>
		public uint PurgeInterval { get; set; } = 5; // 5 minutes
	}
}
