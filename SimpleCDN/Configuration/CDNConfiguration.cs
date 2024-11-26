namespace SimpleCDN.Configuration
{
	public class CDNConfiguration
	{
		/// <summary>
		/// The data root path
		/// </summary>
		public string? DataRoot { get; set; }
		/// <summary>
		/// The maximum size of the in-memory cache in kB
		/// </summary>
		public uint MaxMemoryCacheSize { get; set; } = 500;
	}
}
