using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Extensions.Redis;
using SimpleCDN.Services.Caching;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
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

			builder.Services.ConfigureHttpJsonOptions(options
				=> options.SerializerOptions.TypeInfoResolverChain.Add(ExtraSourceGenerationContext.Default));

			builder.Services.AddHealthChecks()
				.AddCheck("Self", () => HealthCheckResult.Healthy());

			WebApplication app = builder
				.Build();

			// useful for debugging configuration issues
			if (args.Contains("--dump-config"))
			{
				DumpConfiguration(app);
				return;
			}

			app
				.MapEndpoints()
				.Run();
		}

		private static void DumpConfiguration(WebApplication app)
		{
			IOptions<CDNConfiguration> cdnConfig = app.Services.GetRequiredService<IOptions<CDNConfiguration>>();
			IOptions<CacheConfiguration> cacheConfig = app.Services.GetRequiredService<IOptions<CacheConfiguration>>();
			IOptions<InMemoryCacheConfiguration> inMemoryConfig = app.Services.GetRequiredService<IOptions<InMemoryCacheConfiguration>>();
			IOptions<RedisCacheConfiguration> redisConfig = app.Services.GetRequiredService<IOptions<RedisCacheConfiguration>>();
			IOptions<JsonOptions> jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>();

			var jsonConfig = new JsonSerializerOptions(jsonOptions.Value.SerializerOptions)
			{
				WriteIndented = true
			};

#pragma warning disable IL2026, IL3050 // requires unreferenced code, but the TypeInfoResolverChain actually provides the necessary context
			Console.WriteLine("CDN Configuration:");
			Console.WriteLine(JsonSerializer.Serialize(cdnConfig.Value, jsonConfig));
			Console.WriteLine("Cache Configuration:");
			Console.WriteLine(JsonSerializer.Serialize(cacheConfig.Value, jsonConfig));
			Console.WriteLine("InMemory Cache Configuration:");
			Console.WriteLine(JsonSerializer.Serialize(inMemoryConfig.Value, jsonConfig));
			Console.WriteLine("Redis Cache Configuration:");
			Console.WriteLine(JsonSerializer.Serialize(redisConfig.Value, jsonConfig));
#pragma warning restore IL2026, IL3050, CA1869 // requires unreferenced code; reuse JsonSerializerOptions

			Console.WriteLine();
			Console.Write("Selected cache implementation: ");

			var cache = app.Services.GetRequiredService<ICacheImplementationResolver>().Implementation.GetType().Name;
			Console.WriteLine(cache);
		}
	}
}
#pragma warning restore RCS1102 // Make class static
