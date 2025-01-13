namespace SimpleCDN.Configuration
{
	/// <summary>
	/// Represents generic caching settings.
	/// </summary>
	public class CacheConfiguration
	{
		/// <summary>
		/// The maximum time a cache entry may be unused before being deleted, in minutes.
		/// </summary>
		public uint MaxAge { get; set; }
	}
}
