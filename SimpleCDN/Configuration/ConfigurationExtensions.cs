namespace SimpleCDN.Configuration
{
	public static class ConfigurationExtensions
	{
		public static IServiceCollection MapConfiguration(this IServiceCollection services)
		{
			services.AddOptions<CDNConfiguration>()
				.Configure<IConfiguration>((settings, configuration) =>
				{
					if (configuration["CDN_DATA_ROOT"] is string dataRoot)
					{
						settings.DataRoot = dataRoot;
					}

					if (configuration["CDN_CACHE_LIMIT"] is string maxMemoryString
						&& uint.TryParse(maxMemoryString, out uint maxMemory))
					{
						settings.MaxMemoryCacheSize = maxMemory;
					}
				})
				.BindConfiguration("CDN");

			return services;
		}
	}
}
