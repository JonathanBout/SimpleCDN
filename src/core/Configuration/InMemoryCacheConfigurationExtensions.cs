using Microsoft.Extensions.Caching.Distributed;
using SimpleCDN.Services.Caching.Implementations;

namespace SimpleCDN.Configuration
{
	/// <summary>
	/// Provides extension methods for configuring the in-memory cache.
	/// </summary>
	public static class InMemoryCacheConfigurationExtensions
	{
		/// <summary>
		/// Configures the in-memory cache.
		/// </summary>
		public static ISimpleCDNBuilder AddInMemoryCache(this ISimpleCDNBuilder builder, Action<InMemoryCacheConfiguration>? configure = null)
		{
			if (configure is not null)
				builder.Services.Configure(configure);

			builder.Services.AddSingleton<InMemoryCache>()
				.AddHostedService(sp => sp.GetRequiredService<InMemoryCache>())
				.AddSingleton<IDistributedCache>(sp => sp.GetRequiredService<InMemoryCache>());
			builder.UseCacheImplementation<InMemoryCache>();
			return builder;
		}
	}
}
