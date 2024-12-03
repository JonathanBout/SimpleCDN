namespace SimpleCDN.Configuration
{
	public class CacheConfiguration
	{
		public InMemoryCacheConfiguration? InMemory { get; set; }
		public RedisCacheConfiguration? Redis { get; set; }

		public bool Validate()
		{
			// Ensure that exactly one cache provider is configured
			if (InMemory is null == Redis is null)
			{
				return false;
			}
			return true;
		}
	}

	public class InMemoryCacheConfiguration
	{

	}

	public class RedisCacheConfiguration
	{
		public string? Host { get; set; }
		public int Port { get; set; }
		public string? Password { get; set; }
	}
}
