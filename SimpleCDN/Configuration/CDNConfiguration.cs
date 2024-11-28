using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;

namespace SimpleCDN.Configuration
{
	public class CDNConfiguration
	{
		/// <summary>
		/// The data root path
		/// </summary>
		public required string DataRoot { get; set; }
		/// <summary>
		/// The maximum size of the in-memory cache in kB
		/// </summary>
		public uint MaxMemoryCacheSize { get; set; } = 500;

		public string Footer { get; set; } = $"""<a href="https://github.com/jonathanbout/simplecdn">Powered by SimpleCDN</a>""";
		public string PageTitle { get; set; } = "SimpleCDN";

		public bool Validate(ILogger<CDNConfiguration> logger)
		{
			if (string.IsNullOrWhiteSpace(DataRoot))
			{
				logger.LogCritical("DataRoot must be set");
				return false;
			}

			if (MaxMemoryCacheSize == 0)
			{
				logger.LogCritical("MaxMemoryCacheSize must be greater than 0");
				return false;
			}

			return true;	
		}
	}
}
