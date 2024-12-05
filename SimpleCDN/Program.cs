using Microsoft.Extensions.Caching.Distributed;
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
			var builder = WebApplication.CreateSlimBuilder(args);

			// reconfigure the configuration to make sure we're using the right sources in the right order
			builder.Configuration.Sources.Clear();
			builder.Configuration
				.AddEnvironmentVariables()
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddCommandLine(args);

			builder.Services.AddOutputCache(options =>
			{
				var configuration = options.ApplicationServices.GetRequiredService<CDNConfiguration>();

				// output cache is 1/10th of memory cache
				// in bytes               in kilobytes
				options.MaximumBodySize = configuration.MaxMemoryCacheSize * 100;
			});

			builder.Services.MapConfiguration();

			// for now, we use a simple size-limited in-memory cache.
			// In the future, we may want to give options for other cache implementations
			// like Redis or Memcached.
			builder.Services.AddSingleton<IDistributedCache, SizeLimitedCache>();

			builder.Services.AddSingleton<ICDNLoader, CDNLoader>();
			builder.Services.AddSingleton<IIndexGenerator, IndexGenerator>();
			builder.Services.AddSingleton<IPhysicalFileReader, PhysicalFileReader>();
			builder.Services.AddSingleton<ICacheManager, CacheManager>();

			var app = builder.Build();

			app.RegisterCDNEndpoints();
			app.UseOutputCache();

			app.Run();
		}
	}
}