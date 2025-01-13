using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using SimpleCDN.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
