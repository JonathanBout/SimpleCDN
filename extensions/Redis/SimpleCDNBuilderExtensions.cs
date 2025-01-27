using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleCDN.Configuration;
using StackExchange.Redis;

namespace SimpleCDN.Extensions.Redis
{
	/// <summary>
	/// Extensions for the SimpleCDNBuilder to add and configure the Redis cache extension.
	/// </summary>
	public static class SimpleCDNBuilderExtensions
	{
		const string InvalidConfigurationMessage = "See the log meessages for details.";

		/// <summary>
		/// Adds the Redis cache extension to the SimpleCDN.
		/// </summary>
		public static ISimpleCDNBuilder AddRedisCache(this ISimpleCDNBuilder builder, Action<RedisCacheConfiguration> configure)
		{
			builder.Services.AddSingleton<IRedisCacheService, CustomRedisCacheService>();

			builder.Services.AddOptionsWithValidateOnStart<RedisCacheConfiguration>()
				.Configure(configure)
				.Validate<ILogger<RedisCacheConfiguration>>((config, logger) => config.Validate(logger), InvalidConfigurationMessage);

			builder.UseCacheImplementation<IRedisCacheService>();

			return builder;
		}
	}
}
