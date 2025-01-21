using Microsoft.Extensions.Logging;

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
		/// How this client should be identified to Redis. Default is SimpleCDN.
		/// </summary>
		public string ClientName { get; set; } = "SimpleCDN";

		/// <summary>
		/// A prefix to be added to all keys stored in Redis. Default is <c>SimpleCDN::</c>. An empty value is allowed.
		/// </summary>
		public string KeyPrefix { get; set; } = "SimpleCDN::";

		/// <summary>
		/// Validates the configuration settings.
		/// </summary>
		public bool Validate(ILogger<RedisCacheConfiguration> logger)
		{
			bool isValid = true;

			if (string.IsNullOrWhiteSpace(ClientName))
			{
				isValid = false;
				logger.LogCritical($"{nameof(ClientName)} cannot be empty.");
			}

			if (string.IsNullOrWhiteSpace(ConnectionString))
			{
				isValid = false;
				logger.LogCritical($"{nameof(ConnectionString)} cannot be empty.");
			}

			return isValid;
		}
	}
}
