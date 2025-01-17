# SimpleCDN Redis Extension

SimpleCDN Redis Extension is an extension for [SimpleCDN](https://nuget.org/packages/SimpleCDN) that
adds Redis caching support. This extension is useful when you want to run multiple instances of SimpleCDN
in for example a kubernetes cluster.

[The SimpleCDN Docker Image](https://ghcr.io/jonathanbout/simplecdn) comes with Redis support by default.

## Features
- Redis caching

## Configuration
```diff
var builder = WebApplication.CreateBuilder();
var cdnBuilder = builder.Services.AddSimpleCDN();

+cdnBuilder.AddRedisCache(options => { ... });
```

- `options.ConnectionString`: The configuration string for the Redis server. This is a required property.
- `options.ClientName`: How the client should be identified to Redis. Default is `SimpleCDN`. This value can't contain whitespace.
- `options.KeyPrefix`: A string to prepend to all keys SimpleCDN inserts. Default is `SimpleCDN`. An empty value is allowed, meaning no prefix is added.
