using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Endpoints;
using SimpleCDN.Services;

namespace SimpleCDN
{
	public class Program
	{
		private static void Main(string[] args)
		{
			WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

			// reconfigure the configuration to make sure we're using the right sources in the right order
			builder.Configuration.Sources.Clear();
			builder.Configuration
				.AddEnvironmentVariables()
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddCommandLine(args);

			builder.Services.MapConfiguration();

			if (builder.Configuration.GetSection("RedisCache") is IConfigurationSection rcConfig && rcConfig.Exists())
			{
				builder.Services.AddStackExchangeRedisCache(_ => { })
					.Configure<RedisCacheOptions>(options =>
					{
						options.ConfigurationOptions ??= new();
						rcConfig.Bind(options.ConfigurationOptions);
					});
			} else if (builder.Configuration.GetSection("MemoryCache") is IConfigurationSection mcConfig)
			{
				// By default, we use the in-memory cache
				builder.Services.AddSingleton<IDistributedCache, SizeLimitedCache>()
					.Configure<InMemoryCacheConfiguration>(mcConfig);
			}

			builder.Services.AddSingleton<ICDNLoader, CDNLoader>();
			builder.Services.AddSingleton<IIndexGenerator, IndexGenerator>();
			builder.Services.AddSingleton<IPhysicalFileReader, PhysicalFileReader>();
			builder.Services.AddSingleton<ICacheManager, CacheManager>();

			WebApplication app = builder.Build();

			app.RegisterCDNEndpoints();

			app.Run();
		}
	}
}