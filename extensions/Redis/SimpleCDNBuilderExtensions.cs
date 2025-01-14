using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using SimpleCDN.Configuration;

namespace SimpleCDN.Extensions.Redis
{
	public static class SimpleCDNBuilderExtensions
	{
		public static ISimpleCDNBuilder AddRedisCache(this ISimpleCDNBuilder builder, Action<RedisCacheConfiguration> configure)
		{
			builder.Services.AddSingleton<IDistributedCache, CustomRedisCacheService>();
			builder.Services.Configure(configure);

			builder.UseCacheImplementation<CustomRedisCacheService>();

			return builder;
		}
	}
}
