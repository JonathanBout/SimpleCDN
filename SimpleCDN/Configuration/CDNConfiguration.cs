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
		/// The maximum size of the in-memory cache in kB. Default is 500,000 (500 MB)
		/// </summary>
		public uint MaxMemoryCacheSize { get; set; } = 500_000;

		/// <summary>
		/// The footer to be added to the bottom of generated index files. Default is a link to the SimpleCDN GitHub repository
		/// and the text "Powered by SimpleCDN". Supports HTML.
		/// </summary>
		public string Footer { get; set; } = $"""<a href="https://github.com/jonathanbout/simplecdn">Powered by SimpleCDN</a>""";

		/// <summary>
		/// The title of the generated index files. Default is "SimpleCDN". Supports HTML entities.
		/// </summary>
		public string PageTitle { get; set; } = "SimpleCDN";

		/// <summary>
		/// The maximum size of a cached item in kB. Default is 8000 (8 MB)
		/// </summary>
		public int MaxCachedItemSize { get; set; } = 8000;

		/// <summary>
		/// Validates the configuration. Logs to the provided logger with loglevel 'critical' if any validation fails.
		/// </summary>
		/// <param name="logger">The logger to log to</param>
		/// <returns><see langword="true"/> if ok</returns>
		public bool Validate(ILogger<CDNConfiguration> logger)
		{
			bool isValid = true;

			if (string.IsNullOrWhiteSpace(DataRoot))
			{
				logger.LogCritical("DataRoot must be set");
				isValid = false;
			}

			if (MaxMemoryCacheSize == 0)
			{
				logger.LogCritical("MaxMemoryCacheSize must be greater than 0");
				isValid = false;
			}

			if (MaxCachedItemSize < 1 || MaxCachedItemSize > Array.MaxLength)
			{
				logger.LogCritical("MaxCachedItemSize must be greater than 0 and smaller than Array.MaxLength ({max array length} in this case)", Array.MaxLength);
				isValid = false;
			}

			if (MaxCachedItemSize > MaxMemoryCacheSize)
			{
				logger.LogWarning("MaxCachedItemSize should be smaller than MaxMemoryCacheSize. Inserting an item larger than MaxMemoryCacheSize will evict all entries from the cache.");
			}

			// warn if MaxCachedItemSize is greater than a third of system memory
			var gcMemoryInfo = GC.GetGCMemoryInfo();

			if (MaxCachedItemSize > gcMemoryInfo.TotalAvailableMemoryBytes / 3)
			{
				logger.LogWarning("MaxCachedItemSize is greater than a third of available memory ({availableMemory} MB). Are you sure this is a good idea?", gcMemoryInfo.TotalAvailableMemoryBytes / 1_000_000);
			}

			return isValid;
		}
	}
}
