using Microsoft.Extensions.Caching.Distributed;

namespace SimpleCDN.Services.Caching.Implementations
{
	/// <summary>
	/// Resolves the selected cache implementation from the registered services.
	/// </summary>
	internal class CacheImplementationResolver : ICacheImplementationResolver
	{
		public CacheImplementationResolver(IServiceProvider services, Type implementationType)
		{
			var resolved = services.GetService(implementationType)
							?? services.GetServices<IDistributedCache>().FirstOrDefault(s => s.GetType() == implementationType);

			if (resolved is not IDistributedCache dc)
			{
				throw new InvalidOperationException($"The specified cache implementation type '{implementationType}' is not registered.");
			}

			Implementation = dc;
		}

		public CacheImplementationResolver(IDistributedCache implementation) => Implementation = implementation;

		public IDistributedCache Implementation { get; }
	}
}
