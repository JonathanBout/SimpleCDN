using Microsoft.Extensions.Caching.Distributed;

namespace SimpleCDN.Services.Caching
{
	internal interface ICacheImplementationResolver
	{
		IDistributedCache Implementation { get; }
	}
}