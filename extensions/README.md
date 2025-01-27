# SimpleCDN Extensions
SimpleCDN has support for cache providers, other than the built-in in-memory cache. This means anyone can
create an extension for their own cache provider, and use it to extend SimpleCDNs functionalities.
To make sure this is possible, the Redis extension is already built this way.

A few tips to help extension developers:
- The provider should be a singleton service, implementing the `IDistributedCache` interface.
- Registering a typical extension looks like this:
```cs
public static class SimpleCDNBuilderExtensions
{
    public static ISimpleCDNBuilder AddSomeCachingProvider(this ISimpleCDNBuilder builder)
    {
    // === 1. Using the backing type ===
        // register the service as a singleton
        builder.Services.AddSingleton<IDistributedCache, MyCachingProvider>();
        
        // tell SimpleCDN what the service implementation type is
        builder.UseCacheImplementation<MyCachingProvider>();

    // === 2. Using the public-facing type ===
        builder.Services.AddSingleton<IMyCachingProvider, MyCachingProvider>();
        builder.UseCachingImplementation<IMyCachingProvider>();

    // === 3. Using a resolver delegate ===
        builder.Services.AddSingleton<MyCachingProviderWrapper>();
        builder.UseCachingImplementation(sp => sp.GetRequiredService<MyCachingProviderWrapper>().GetMyCachingProvider());

        return builder;
    }
}
```
For a real-life example, look at [Redis/SimpleCDNBuilderExtensions.cs](https://github.com/jonathanbout/simplecdn/tree/main/extensions/Redis/SimpleCDNBuilderExtensions.cs).
