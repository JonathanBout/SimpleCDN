namespace SimpleCDN.Configuration
{
	public class RedisCacheConfiguration
	{
		public string ConnectionString { get; set; } = "localhost";
		public string InstanceName { get; set; } = "SimpleCDN";
	}
}
