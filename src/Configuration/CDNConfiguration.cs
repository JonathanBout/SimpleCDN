namespace SimpleCDN.Configuration
{
	/// <summary>
	/// Represents the configuration for the CDN used by SimpleCDN.
	/// </summary>
	public class CDNConfiguration
	{
		/// <summary>
		/// The data root path
		/// </summary>
		public string DataRoot { get; set; } = "";

		/// <summary>
		/// The footer to be added to the bottom of generated index files. Default is a link to the SimpleCDN GitHub repository
		/// and the text "Powered by SimpleCDN". Supports HTML.
		/// </summary>
		public string Footer { get; set; } = """<a href="https://github.com/jonathanbout/simplecdn">Powered by SimpleCDN</a>""";

		/// <summary>
		/// The title of the generated index files. Default is "SimpleCDN". Supports HTML entities.
		/// </summary>
		public string PageTitle { get; set; } = "SimpleCDN";

		/// <summary>
		/// The maximum size of a cached item in kB. Default is 8000 (8 MB)
		/// </summary>
		public int MaxCachedItemSize { get; set; } = 8_000;

		private bool _showDotFiles;

		/// <summary>
		/// Whether to show files starting with a dot (e.g. .gitignore) in the index. Default is false.
		/// This is only relevant if <see cref="AllowDotFileAccess"/> is set to true.
		/// This setting does not control access to dotfiles, only their visibility in the index.
		/// <br/>On Windows, this setting also affects files with the <see cref="FileAttributes.Hidden"/> attribute.
		/// </summary>
		public bool ShowDotFiles
		{
			get => _showDotFiles && AllowDotFileAccess;
			set => _showDotFiles = value;
		}

		/// <summary>
		/// Whether to block web crawlers and other bots from indexing the CDN files. Default is true.
		/// </summary>
		public bool BlockRobots { get; set; } = true;

		/// <summary>
		/// Whether to allow access to files and directories starting with a dot (e.g. .ssl). Default is false.
		/// <br/>On Windows, this setting also affects files with the <see cref="FileAttributes.Hidden"/> attribute.
		/// </summary>
		public bool AllowDotFileAccess { get; set; }

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
				logger.LogCritical($"{nameof(DataRoot)} must be set");
				isValid = false;
			}

			if (MaxCachedItemSize < 1 || MaxCachedItemSize > Array.MaxLength)
			{
				logger.LogCritical("MaxCachedItemSize must be greater than 0 and smaller than Array.MaxLength ({max array length} in this case)", Array.MaxLength);
				isValid = false;
			}

			// warn if MaxCachedItemSize is greater than a third of system memory
			GCMemoryInfo gcMemoryInfo = GC.GetGCMemoryInfo();

			if (MaxCachedItemSize > gcMemoryInfo.TotalAvailableMemoryBytes / 3)
			{
				logger.LogWarning($$"""{{nameof(MaxCachedItemSize)}} is greater than a third of available memory ({availableMemory} MB). Are you sure this is a good idea?""", gcMemoryInfo.TotalAvailableMemoryBytes / 1_000_000);
			}

			return isValid;
		}
	}
}
