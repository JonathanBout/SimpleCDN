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
				.BindConfiguration("CDN")
				.Validate((CDNConfiguration config, ILogger<CDNConfiguration> logger) => config.Validate(logger), InvalidConfigurationMessage);

			builder.Services
				.AddOptions<CacheConfiguration>()
				.BindConfiguration("Cache");

			IConfigurationSection redisSection = configuration.GetSection("Cache:Redis");
			IConfigurationSection inMemorySection = configuration.GetSection("Cache:InMemory");

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
