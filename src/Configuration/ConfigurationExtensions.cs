using Microsoft.Extensions.Caching.Distributed;
using SimpleCDN.Helpers;
using SimpleCDN.Services;
using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Caching.Implementations;
using SimpleCDN.Services.Compression;
using SimpleCDN.Services.Compression.Implementations;
using SimpleCDN.Services.Implementations;

namespace SimpleCDN.Configuration
{
	/// <summary>
	/// Provides extension methods for configuring SimpleCDN.
	/// </summary>
	public static class ConfigurationExtensions
	{
		const string InvalidConfigurationMessage = "See the log meessages for details.";

		/// <summary>
		/// Maps the <see cref="CDNConfiguration"/> and <see cref="CacheConfiguration"/> classes to the
		/// application configuration. This can only be used when running SimpleCDN as a standalone application.
		/// </summary>
		internal static ISimpleCDNBuilder MapConfiguration(this ISimpleCDNBuilder builder)
		{
			CDNConfiguration? cdnConfig = builder.Configuration.GetSection("CDN").Get<CDNConfiguration>();
			CacheConfiguration? cacheConfig = builder.Configuration.GetSection("Cache").Get<CacheConfiguration>();

			switch (cacheConfig?.Type ?? CacheConfiguration.CacheType.InMemory)
			{
				case CacheConfiguration.CacheType.InMemory:
					builder.Services.AddSingleton<SizeLimitedCache>()
						// the cache is a hosted service as it needs to purge expired items
						.AddHostedService(sp => sp.GetRequiredService<SizeLimitedCache>())
						.AddSingleton<IDistributedCache>(sp => sp.GetRequiredService<SizeLimitedCache>())
						.Configure<InMemoryCacheConfiguration>(builder.Configuration.GetSection("Cache:InMemory"));
					break;
				case CacheConfiguration.CacheType.Redis when cacheConfig is { Redis.ConnectionString: not null }:
					builder.Services.AddSingleton<IDistributedCache, CustomRedisCacheService>();
					break;
				default:
					builder.Services.AddSingleton<IDistributedCache, DisabledCache>();
					break;
			}

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

			builder.Services.AddOptions<CacheConfiguration>()
				.BindConfiguration("Cache");

			return builder;
		}

		/// <summary>
		/// Adds the SimpleCDN services to the application and configures them using the provided configuration.
		/// </summary>
		public static ISimpleCDNBuilder AddSimpleCDN(this IHostApplicationBuilder builder, Action<CDNConfiguration> configure)
		{
			return builder.AddSimpleCDN()
				.Configure(configure);
		}

		/// <summary>
		/// Adds the SimpleCDN services to the application.
		/// </summary>
		public static ISimpleCDNBuilder AddSimpleCDN(this IHostApplicationBuilder builder)
		{
			builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.Default));

			builder.Services.AddSingleton<ICDNLoader, CDNLoader>();
			builder.Services.AddSingleton<IIndexGenerator, IndexGenerator>();
			builder.Services.AddSingleton<IPhysicalFileReader, PhysicalFileReader>();
			builder.Services.AddSingleton<ICacheManager, CacheManager>();

			builder.Services.AddSingleton<ICompressor, BrotliCompressor>();
			builder.Services.AddSingleton<ICompressor, GZipCompressor>();
			builder.Services.AddSingleton<ICompressor, DeflateCompressor>();

			builder.Services.AddSingleton<ICompressionManager, CompressionManager>();

			builder.Services.AddOptionsWithValidateOnStart<CDNConfiguration>();
			builder.Services.AddOptionsWithValidateOnStart<CacheConfiguration>();

			return new SimpleCDNBuilder(builder);
		}

		private class SimpleCDNBuilder(IHostApplicationBuilder applicationBuilder) : ISimpleCDNBuilder
		{
			private readonly IHostApplicationBuilder _applicationBuilder = applicationBuilder;

			IServiceCollection ISimpleCDNBuilder.Services => _applicationBuilder.Services;
			IConfigurationManager ISimpleCDNBuilder.Configuration => _applicationBuilder.Configuration;

			public ISimpleCDNBuilder Configure(Action<CDNConfiguration> configure)
			{
				_applicationBuilder.Services.Configure(configure);
				return this;
			}

			public ISimpleCDNBuilder ConfigureCaching(Action<CacheConfiguration> configure)
			{
				_applicationBuilder.Services.Configure(configure);
				return this;
			}

			ISimpleCDNBuilder ISimpleCDNBuilder.Configure(Action<CDNConfiguration> configure) => throw new NotImplementedException();
			ISimpleCDNBuilder ISimpleCDNBuilder.ConfigureCaching(Action<CacheConfiguration> configure) => throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Represents a SimpleCDN builder.
	/// </summary>
	public interface ISimpleCDNBuilder
	{
		internal IServiceCollection Services { get; }
		internal IConfigurationManager Configuration { get; }
		/// <summary>
		/// Configures generic SimpleCDN settings.
		/// </summary>
		ISimpleCDNBuilder Configure(Action<CDNConfiguration> configure);

		/// <summary>
		/// Configures the caching used by SimpleCDN.
		/// </summary>
		ISimpleCDNBuilder ConfigureCaching(Action<CacheConfiguration> configure);
	}
}
