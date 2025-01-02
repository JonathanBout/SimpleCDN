using Microsoft.Extensions.Caching.Distributed;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Endpoints;
using SimpleCDN.Services;
using SimpleCDN.Services.Compression;

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

			builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolver = SourceGenerationContext.Default);

			// for now, we use a simple size-limited in-memory cache.
			// In the future, we may want to give options for other cache implementations
			// like Redis or Memcached.
			builder.Services.AddSingleton<IDistributedCache, SizeLimitedCache>();

			builder.Services.AddSingleton<ICDNLoader, CDNLoader>();
			builder.Services.AddSingleton<IIndexGenerator, IndexGenerator>();
			builder.Services.AddSingleton<IPhysicalFileReader, PhysicalFileReader>();
			builder.Services.AddSingleton<ICacheManager, CacheManager>();

			builder.Services.AddSingleton<ICompressor, BrotliCompressor>();
			builder.Services.AddSingleton<ICompressor, GZipCompressor>();
			builder.Services.AddSingleton<ICompressor, DeflateCompressor>();

			builder.Services.AddSingleton<ICompressionManager, CompressionManager>();

			WebApplication app = builder.Build();

			app.RegisterCDNEndpoints();

			app.Use(async (ctx, next) =>
			{
				ctx.Response.Headers.Server = "SimpleCDN";
				ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
				await next();
			});

			app.Run();
		}
	}
}
