using SimpleCDN.Configuration;
using SimpleCDN.Extensions.Redis;

namespace SimpleCDN.Standalone
{
	public static class ApplicationBuilderExtensions
	{
		/// <summary>
		/// Maps the <see cref="CDNConfiguration"/> and <see cref="CacheConfiguration"/> classes to the
		/// application configuration.
		/// </summary>
		internal static ISimpleCDNBuilder MapConfiguration(this ISimpleCDNBuilder builder, IConfiguration configuration)
		{
			builder.Services
				.AddOptions<CDNConfiguration>()
				.BindConfiguration("CDN");

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
				case CacheType.Unspecified:
					// if no provider is explicitly specified, we look at what is configured,
					// to determine which cache provider to use.
					if (redisSection.Exists())
					{
						goto case CacheType.Redis;
					}
					goto case CacheType.InMemory;
			}

			return builder;
		}
	}

	public enum CacheType
	{
		/// <summary>
		/// The cache type is unspecified. The cache type will be determined by the configuration.
		/// If Redis is configured, Redis will be used. Otherwise, InMemory will be used.
		/// </summary>
		Unspecified = 0,
		InMemory = 1,
		Redis = 2,
		Disabled = 3
	}
}
