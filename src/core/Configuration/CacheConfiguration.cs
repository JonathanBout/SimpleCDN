using System.ComponentModel;

namespace SimpleCDN.Configuration
{
	/// <summary>
	/// Represents generic caching settings.
	/// </summary>
	public class CacheConfiguration
	{
		/// <summary>
		/// The maximum time a cache entry may be unused before being deleted, in minutes. Default is 1 hour.
		/// Set to <see cref="TimeSpan.Zero"/> for no expiration.
		/// </summary>
		public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(1);

		/// <summary>
		/// Whether to suppress warnings about the maximum age being less than 30 seconds.
		/// </summary>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool SuppressMaxAgeWarning { get; set; }

		/// <summary>
		/// The maximum size of a cached item in kB. Default is 8000 (8 MB).
		/// </summary>
		public uint MaxItemSize { get; set; } = 8_000;

		/// <summary>
		/// Whether to suppress warnings about the maximum item size being greater than a third of system memory.
		/// </summary>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool SuppressMemorySizeWarning { get; set; }

		/// <summary>
		/// Validates the configuration. Logs to the provided logger with loglevel 'critical' if any validation fails.
		/// </summary>
		/// <param name="logger">The logger to log to.</param>
		/// <returns><see langword="true"/> if ok.</returns>
		public bool Validate(ILogger<CacheConfiguration> logger)
		{
			bool isValid = true;

			if (MaxItemSize < 1 || MaxItemSize > Array.MaxLength)
			{
				logger.LogCritical("MaxCachedItemSize must be greater than 0 and smaller than Array.MaxLength ({max array length} in this case)", Array.MaxLength);
				isValid = false;
			}

			// warn if MaxCachedItemSize is greater than a third of system memory
			GCMemoryInfo gcMemoryInfo = GC.GetGCMemoryInfo();

			if (!SuppressMemorySizeWarning && MaxItemSize > gcMemoryInfo.TotalAvailableMemoryBytes / 3)
			{
				logger.LogWarning($$"""{{nameof(MaxItemSize)}} is greater than a third of available memory ({availableMemory} MB). Are you sure this is a good idea? To suppress this warning, set {{nameof(SuppressMemorySizeWarning)}} to true.""", gcMemoryInfo.TotalAvailableMemoryBytes / 1_000_000);
			}

			if (MaxAge != TimeSpan.Zero && !SuppressMaxAgeWarning && MaxAge < TimeSpan.FromSeconds(30))
			{
				logger.LogWarning($"{nameof(MaxAge)} is less than 30 seconds. Are you sure this is right? To suppress this warning, set {nameof(SuppressMaxAgeWarning)} to true");
			}

			return isValid;
		}
	}
}
