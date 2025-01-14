namespace SimpleCDN.Extensions.Redis
{
	/// <summary>
	/// Represents the configuration for the Redis cache used by SimpleCDN.
	/// </summary>
	public class RedisCacheConfiguration
	{
		/// <summary>
		/// The connection string to the Redis server. Default is localhost.
		/// </summary>
		public string ConnectionString { get; set; } = "localhost";

		/// <summary>
		/// The name of the Redis instance. Default is SimpleCDN.
		/// </summary>
		public string InstanceName { get; set; } = "SimpleCDN";
	}
}
