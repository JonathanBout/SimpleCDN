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
			services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.Default));

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

			services.AddOptionsWithValidateOnStart<CDNConfiguration>();

			return new SimpleCDNBuilder(services);
		}

		private class SimpleCDNBuilder : ISimpleCDNBuilder
		{
			private Type _cacheImplementationType = typeof(DisabledCache);

			public SimpleCDNBuilder(IServiceCollection services)
			{
				Services = services;

				Services.AddSingleton<IDistributedCache, DisabledCache>();
				Services.AddSingleton<ICacheManager, CacheManager>();
				Services.AddSingleton<ICacheImplementationResolver>(sp => new CacheImplementationResolver(sp, _cacheImplementationType));
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
