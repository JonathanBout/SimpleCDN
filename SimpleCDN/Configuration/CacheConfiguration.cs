namespace SimpleCDN.Configuration
{
	public class InMemoryCacheConfiguration
	{
		/// <summary>
		/// The maximum size of the in-memory cache in kB. Default is 500,000 (500 MB)
		/// </summary>
		public uint MaxSize { get; set; } = 500_000; // 500 MB
	}
}
