using Microsoft.Extensions.Caching.Distributed;

namespace SimpleCDN.Services.Caching.Implementations
{
	/// <summary>
	/// Resolves the selected cache implementation from the registered services.
	/// </summary>
	internal class CacheImplementationResolver(IServiceProvider services, Type implementationType) : ICacheImplementationResolver
	{
		private IDistributedCache? _impl;
		public IDistributedCache Implementation
		{
			get
			{
				_impl ??= services.GetServices<IDistributedCache>().FirstOrDefault(s => s.GetType() == implementationType);

				if (_impl is null)
					throw new InvalidOperationException($"The specified cache implementation ({implementationType.Name}) is not registered.");

				return _impl;
			}
		}
	}
}
