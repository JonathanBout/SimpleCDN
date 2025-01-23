using SimpleCDN.Configuration;
using TomLonghurst.ReadableTimeSpan;

namespace SimpleCDN.Standalone
{
#pragma warning disable RCS1102 // This class can't be static because the integration tests want it as a type argument
	public class Program
	{
		private static void Main(string[] args)
		{
			ReadableTimeSpan.EnableConfigurationBinding();

			WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

			// reconfigure the configuration to make sure we're using the right sources in the right order
			builder.Configuration.Sources.Clear();
			builder.Configuration
				.AddEnvironmentVariables()
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddCommandLine(args);

			builder.Services.AddSimpleCDN()
				.MapConfiguration(builder.Configuration);

			WebApplication app = builder
				.Build();
#if DEBUG
			builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Add(ExtraSourceGenerationContext.Default));
			// useful for debugging configuration issues
			if (args.Contains("--dump-config"))
			{
				if (RuntimeFeature.IsDynamicCodeSupported)
				{
					DumpConfiguration(app);
				}
				return;
			}
#endif

			app
				.MapEndpoints()
				.Run();
		}

#if DEBUG
		private static void DumpConfiguration(WebApplication app)
		{
			IOptions<CDNConfiguration> cdnConfig = app.Services.GetRequiredService<IOptions<CDNConfiguration>>();
			IOptions<CacheConfiguration> cacheConfig = app.Services.GetRequiredService<IOptions<CacheConfiguration>>();
			IOptions<InMemoryCacheConfiguration> inMemoryConfig = app.Services.GetRequiredService<IOptions<InMemoryCacheConfiguration>>();
			IOptions<RedisCacheConfiguration> redisConfig = app.Services.GetRequiredService<IOptions<RedisCacheConfiguration>>();

			var jsonConfig = new JsonSerializerOptions { WriteIndented = true };

			Console.WriteLine("CDN Configuration:");
			Console.WriteLine(JsonSerializer.Serialize(cdnConfig.Value, ExtraSourceGenerationContext.Default.CDNConfiguration));
			Console.WriteLine("Cache Configuration:");
			Console.WriteLine(JsonSerializer.Serialize(cacheConfig.Value, ExtraSourceGenerationContext.Default.CacheConfiguration));
			Console.WriteLine("InMemory Cache Configuration:");
			Console.WriteLine(JsonSerializer.Serialize(inMemoryConfig.Value, ExtraSourceGenerationContext.Default.InMemoryCacheConfiguration));
			Console.WriteLine("Redis Cache Configuration:");
			Console.WriteLine(JsonSerializer.Serialize(redisConfig.Value, ExtraSourceGenerationContext.Default.RedisCacheConfiguration));

			Console.WriteLine();
			Console.Write("Selected cache implementation: ");

			var cache = app.Services.GetRequiredService<ICacheImplementationResolver>().Implementation.GetType().Name;
			Console.WriteLine(cache);
		}
#endif

	}
}
#pragma warning restore RCS1102 // Make class static
