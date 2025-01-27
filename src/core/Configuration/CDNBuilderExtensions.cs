using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleCDN.Services;
using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Caching.Implementations;
using SimpleCDN.Services.Compression;
using SimpleCDN.Services.Compression.Implementations;
using SimpleCDN.Services.Implementations;
using System.ComponentModel;
using System.Text.Json;

namespace SimpleCDN.Configuration
{
	/// <summary>
	/// Provides extension methods for configuring SimpleCDN.
	/// </summary>
	public static class CDNBuilderExtensions
	{
		const string InvalidConfigurationMessage = "See the log meessages for details.";

		/// <summary>
		/// Adds the SimpleCDN services to the application and configures them using the provided configuration.
		/// </summary>
		public static ISimpleCDNBuilder AddSimpleCDN(this IServiceCollection services, Action<CDNConfiguration> configure)
		{
			return services.AddSimpleCDN()
				.Configure(configure);
		}

		/// <summary>
		/// Adds the SimpleCDN services to the application.
		/// </summary>
		public static ISimpleCDNBuilder AddSimpleCDN(this IServiceCollection services)
		{
			services.AddHttpContextAccessor();
			services.AddScoped<ICDNContext, CDNContext>();
			services.AddScoped<IIndexGenerator, IndexGenerator>();
			services.AddScoped<ICDNLoader, CDNLoader>();

			services.AddSingleton<ISystemFileReader, SystemFileReader>();
			services.AddSingleton<IPhysicalFileReader, PhysicalFileReader>();
			services.AddSingleton<ICompressionManager, CompressionManager>();
			services.AddSingleton<ICompressor, BrotliCompressor>();
			services.AddSingleton<ICompressor, GZipCompressor>();
			services.AddSingleton<ICompressor, DeflateCompressor>();

			services.AddOptionsWithValidateOnStart<CDNConfiguration>()
				.Validate<ILogger<CDNConfiguration>>(static (config, logger) => config.Validate(logger), InvalidConfigurationMessage);

			services.AddOptionsWithValidateOnStart<CacheConfiguration>()
				.Validate<ILogger<CacheConfiguration>>(static (config, logger) => config.Validate(logger), InvalidConfigurationMessage);

			return new SimpleCDNBuilder(services);
		}

		private class SimpleCDNBuilder : ISimpleCDNBuilder
		{
			public SimpleCDNBuilder(IServiceCollection services)
			{
				Services = services;

				Services.AddSingleton<IDistributedCache, DisabledCache>();
				Services.AddSingleton<ICacheManager, CacheManager>();
				UseCacheImplementation<DisabledCache>();
				Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.Default));
			}

			public IServiceCollection Services { get; }

			public ISimpleCDNBuilder Configure(Action<CDNConfiguration> configure)
			{
				Services.Configure(configure);
				return this;
			}

			public ISimpleCDNBuilder ConfigureCaching(Action<CacheConfiguration> configure)
			{
				Services.Configure(configure);
				return this;
			}

			public ISimpleCDNBuilder DisableCaching() => UseCacheImplementation<DisabledCache>();

			private void RemoveCacheImplementations()
			{
				Services.RemoveAll<ICacheImplementationResolver>();
			}

			public ISimpleCDNBuilder UseCacheImplementation<TImplementation>() where TImplementation : IDistributedCache
			{
				RemoveCacheImplementations();

				Services.AddSingleton<ICacheImplementationResolver>(sp => new CacheImplementationResolver(sp, typeof(TImplementation)));
				return this;
			}

			public ISimpleCDNBuilder UseCacheImplementation<TImplementation>(Func<IServiceProvider, TImplementation> resolve) where TImplementation : IDistributedCache
			{
				RemoveCacheImplementations();

				Services.AddSingleton<ICacheImplementationResolver>(sp => new CacheImplementationResolver(resolve(sp)));
				return this;
			}

			public ISimpleCDNBuilder UseCacheImplementation<TImplementation>(TImplementation implementation) where TImplementation : IDistributedCache
			{
				return UseCacheImplementation(_ => implementation);
			}
		}
	}

	/// <summary>
	/// Represents a SimpleCDN builder.
	/// </summary>
	public interface ISimpleCDNBuilder
	{
		/// <summary>
		/// The service collection used to register services.
		/// </summary>
		IServiceCollection Services { get; }

		/// <summary>
		/// Configures generic SimpleCDN settings.
		/// </summary>
		ISimpleCDNBuilder Configure(Action<CDNConfiguration> configure);

		/// <summary>
		/// Configures generic caching settings.
		/// </summary>
		ISimpleCDNBuilder ConfigureCaching(Action<CacheConfiguration> configure);

		/// <summary>
		/// Disables caching. This may be overridden again when you register a cache implementation afterwards.
		/// </summary>
		ISimpleCDNBuilder DisableCaching();

		/// <summary>
		/// Configures SimpleCDN to use the specified cache implementation. This should be used when a custom cache implementation is used.
		/// </summary>
		/// <typeparam name="TImplementation">
		/// The serivce type to use as the cache implementation. Must implement <see cref="IDistributedCache"/>.
		/// </typeparam>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		ISimpleCDNBuilder UseCacheImplementation<TImplementation>() where TImplementation : IDistributedCache;

		/// <summary>
		/// Configures SimpleCDN to use the specified cache implementation. This should be used when a custom cache implementation is used.
		/// </summary>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		ISimpleCDNBuilder UseCacheImplementation<TImplementation>(Func<IServiceProvider, TImplementation> provider) where TImplementation : IDistributedCache;

		/// <summary>
		/// Configures SimpleCDN to use the specified cache implementation. This should be used when a custom cache implementation is used.
		/// </summary>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		ISimpleCDNBuilder UseCacheImplementation<TImplementation>(TImplementation implementation) where TImplementation : IDistributedCache;
	}
}
