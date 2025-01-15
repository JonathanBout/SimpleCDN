# ![](https://raw.githubusercontent.com/JonathanBout/SimpleCDN/refs/heads/main/src/core/SystemFiles/logo.svg) SimpleCDN

SimpleCDN is one of the simplest and easiest-to-use CDN servers. To use it, simply add two lines
of code to your startup code:

```csharp
var cdnBuilder = builder.AddSimpleCDN(options => options.DataRoot = "/var/static");

// ...

app.MapGroup("/cdn").MapSimpleCDN();
```

This will map the SimpleCDN endpoint to `/cdn` and serve files from `/var/static`.

## Features
- In-memory caching
- Automatic compression (currently supported: gzip, deflate, brotli)
- Redis caching for multiple instances or a cluster, with the
  [SimpleCDN.Extensions.Redis](https://www.nuget.org/packages/SimpleCDN.Extensions.Redis/) package.

SimpleCDN is also available as a standalone application with a docker container:
	[ghcr.io/jonathanbout/simplecdn](https://ghcr.io/jonathanbout/simplecdn)


## Configuration

### General configuration
This configuration is for general settings for the CDN server.
```csharp
var cdnBuilder = builder.AddSimpleCDN(options => { ... });
// or
cdnBuilder.Configure(options => { ... });
```

- `options.DataRoot`: The root directory to serve files from. This is a required property.
- `options.Footer`: Set a custom footer for generated index pages. Default is `Powered by SimpleCDN`,
  with a link to the github repo.
- `options.PageTitle`: Set a custom title for generated index pages. Default is `SimpleCDN`.
- `options.MaxCachedItemSize`: The maximum size of a file to cache in kB, other files will be streamed
  directly from disk. Default is `8_000` (8MB).
- `options.AllowDotfileAccess`: Whether to allow access to files starting with a dot. Default is `false`.
- `options.ShowDotFiles`: Whether to show files starting with a dot. Default is `false`.
  If `options.AllowDotfileAccess` is `false`, this option is ignored.
- `options.BlockRobots`: Whether to block robots from indexing the CDN. Default is `true`.

### General caching configuration
This configuration is used by the Cache Manager and uses it to configure the caching provider.
```csharp
cdnBuilder.ConfigureCaching(options => { ... });
```
- `options.MaxAge`: The maximum time a file can be unused before it is removed from the cache, in minutes.
  Default is `60` (1 hour).

### In-memory caching configuration
This configuration is used by the in-memory cache provider.
```csharp
cdnBuilder.ConfigureInMemoryCaching(options => { ... });
```
- `options.MaxSize`: The maximum size of the cache in kB. Default is `500_000` (500MB). When this limit
  is passed, the least recently used files are removed from the cache until the size is below the limit.
- `options.PurgeInterval`: The interval at which the cache is purged of unused files, in minutes.
  Default is `5`. Set to `0` to disable purging. Note that disabling purging means the MaxAge cache
  configuration property will not be respected.
