# ![](https://raw.githubusercontent.com/JonathanBout/SimpleCDN/refs/heads/main/src/core/SystemFiles/logo.svg) SimpleCDN Redis Extension

SimpleCDN Redis Extension is an extension for [SimpleCDN](https://nuget.org/packages/SimpleCDN) that
adds Redis caching support. This extension is useful when you want to run multiple instances of SimpleCDN
in for example a kubernetes cluster.

## Features
- Redis caching

## Configuration
```csharp
builder.Services.AddSimpleCDN(...)
	.AddRedisCache(options => { ... });
```

- `options.Configuration`: The configuration string for the Redis server. This is a required property.
- `options.InstanceName`: The instance name to use for the cache. Default is `SimpleCDN`.