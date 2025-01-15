using SimpleCDN.Configuration;
using SimpleCDN.Extensions.Redis;

namespace SimpleCDN.Standalone
{
	public static class ApplicationBuilderExtensions
	{
		const string InvalidConfigurationMessage = "See the log meessages for details.";

		/// <summary>
		/// Maps the <see cref="CDNConfiguration"/> and <see cref="CacheConfiguration"/> classes to the
		/// application configuration.
		/// </summary>
		internal static ISimpleCDNBuilder MapConfiguration(this ISimpleCDNBuilder builder, IConfiguration configuration)
		{
			CDNConfiguration? cdnConfig = configuration.GetSection("CDN").Get<CDNConfiguration>();

			builder.Services
				.AddOptions<CDNConfiguration>()
				.Configure<IConfiguration>((settings, configuration) =>
				{
					IConfigurationSection cdnSection = configuration.GetSection("CDN");

					// First, try to bind to ENV variables
					settings.DataRoot = configuration["CDN_DATA_ROOT"] ?? settings.DataRoot;

					// Then, try to bind to the configuration file (possibly overwriting environment variables)
					cdnSection.Bind(settings);

					// Finally, try to bind to the command line arguments (possibliy overwriting environment variables and configuration file)
					settings.DataRoot = configuration["data-root"] ?? settings.DataRoot;
				})
				.Validate((CDNConfiguration config, ILogger<CDNConfiguration> logger) => config.Validate(logger), InvalidConfigurationMessage);

			builder.Services
				.AddOptions<CacheConfiguration>()
				.Configure<IConfiguration>((settings, configuration) =>
				{
					IConfigurationSection cacheSection = configuration.GetSection("Cache");
					// First, try to bind to ENV variables
					if (configuration["CDN_CACHE_MAX_AGE"] is string maxAgeEnvironmentString && uint.TryParse(maxAgeEnvironmentString, out uint maxAgeEnvironment))
						settings.MaxAge = maxAgeEnvironment;

					// Then, try to bind to the configuration file (possibly overwriting environment variables)
					cacheSection.Bind(settings);

					// Finally, try to bind to the command line arguments (possibliy overwriting environment variables and configuration file)
					if (configuration["max-age"] is string maxAgeArgumentString && uint.TryParse(maxAgeArgumentString, out uint maxAgeArgument))
						settings.MaxAge = maxAgeArgument;
				});

			IConfigurationSection redisSection = configuration.GetSection("Redis");
			IConfigurationSection inMemorySection = configuration.GetSection("InMemory");

			switch (configuration.GetSection("Cache:Type").Get<CacheType>())
			{
				case CacheType.Redis:
					builder.AddRedisCache(config => redisSection.Bind(config));
					break;
				case CacheType.InMemory:
					builder.AddInMemoryCache(config => inMemorySection.Bind(config));
					break;
			}

			return builder;
		}
	}

	public enum CacheType
	{
		// fallback to in-memory cache if no type is specified
		InMemory = 0,
		Redis = 1,
		Disabled = 2
	}
}
