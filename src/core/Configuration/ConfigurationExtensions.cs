using Microsoft.Extensions.Caching.Distributed;
using SimpleCDN.Services;
using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Caching.Implementations;
using SimpleCDN.Services.Compression;
using SimpleCDN.Services.Compression.Implementations;
using SimpleCDN.Services.Implementations;
using System.ComponentModel;

namespace SimpleCDN.Configuration
{
	/// <summary>
	/// Provides extension methods for configuring SimpleCDN.
	/// </summary>
	public static class ConfigurationExtensions
	{
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

			builder.Services.AddHttpContextAccessor();
			builder.Services.AddScoped<ICDNContext, CDNContext>();
			builder.Services.AddScoped<IIndexGenerator, IndexGenerator>();
			builder.Services.AddScoped<ICDNLoader, CDNLoader>();

			builder.Services.AddSingleton<ISystemFileReader, SystemFileReader>();
			builder.Services.AddSingleton<IPhysicalFileReader, PhysicalFileReader>();
			builder.Services.AddSingleton<ICompressionManager, CompressionManager>();
			builder.Services.AddSingleton<ICompressor, BrotliCompressor>();
			builder.Services.AddSingleton<ICompressor, GZipCompressor>();
			builder.Services.AddSingleton<ICompressor, DeflateCompressor>();

			builder.Services.AddOptionsWithValidateOnStart<CDNConfiguration>();

			return new SimpleCDNBuilder(builder);
		}

		private class SimpleCDNBuilder : ISimpleCDNBuilder
		{
			private readonly IHostApplicationBuilder _applicationBuilder;

			private Type _cacheImplementationType = typeof(DisabledCache);

			public SimpleCDNBuilder(IHostApplicationBuilder applicationBuilder)
			{
				_applicationBuilder = applicationBuilder;

				Services.AddSingleton<IDistributedCache, DisabledCache>();
				Services.AddSingleton<ICacheManager, CacheManager>();
				Services.AddSingleton<ICacheImplementationResolver>(sp => new CacheImplementationResolver(sp, _cacheImplementationType));
			}

			public IServiceCollection Services => _applicationBuilder.Services;
			public IConfigurationManager Configuration => _applicationBuilder.Configuration;

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

			public ISimpleCDNBuilder UseCacheImplementation<TImplementation>() where TImplementation : IDistributedCache
			{
				/*
				 * - if the service is registered as IDistributedCache
				 *	- and
				 *		| the implementation type is the specified type
				 *		| or the implementation instance is the specified type
				 *		| or the implementation factory is not null (we can't check the type)
				 *	
				 *	then we can use the specified cache implementation.
				 */
				if (!Services.Any(s => s.ServiceType == typeof(IDistributedCache)
										&& (s.ImplementationType == typeof(TImplementation)
											|| s.ImplementationInstance?.GetType() == typeof(TImplementation)
											|| s.ImplementationFactory is not null)))
				{
					throw new InvalidOperationException("The specified cache implementation is not registered.");
				}

				_cacheImplementationType = typeof(TImplementation);

				return this;
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
		/// The configuration manager used to access configuration settings.
		/// </summary>
		IConfigurationManager Configuration { get; }

		/// <summary>
		/// Configures generic SimpleCDN settings.
		/// </summary>
		ISimpleCDNBuilder Configure(Action<CDNConfiguration> configure);

		/// <summary>
		/// Configures generic caching settings.
		/// </summary>
		ISimpleCDNBuilder ConfigureCaching(Action<CacheConfiguration> configure);

		/// <summary>
		/// Configures SimpleCDN to use the specified cache implementation. This should be used when a custom cache implementation is used.
		/// </summary>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		ISimpleCDNBuilder UseCacheImplementation<TImplementation>() where TImplementation : IDistributedCache;
	}
}
