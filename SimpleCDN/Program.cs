using Microsoft.Extensions.Caching.Distributed;
using SimpleCDN;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Endpoints;
using SimpleCDN.Services;
using System.IO.Compression;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.Sources.Clear();

builder.Configuration
	.AddEnvironmentVariables()
	.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
	.AddCommandLine(args);

builder.Services.MapConfiguration();

//builder.Services.AddStackExchangeRedisCache(options =>
//{
//	options.Configuration = builder.Configuration["Cache:Redis:Host"];
//	options.InstanceName = "SimpleCDN";

//	options.ConfigurationOptions ??= new();

//	builder.Configuration.Bind("Cache:Redis", options.ConfigurationOptions);
//});

builder.Services.AddSingleton<IDistributedCache, SizeLimitedCache>();

builder.Services.AddSingleton<ICDNLoader, CDNLoader>();
builder.Services.AddSingleton<IIndexGenerator, IndexGenerator>();
builder.Services.AddSingleton<IPhysicalFileReader, PhysicalFileReader>();
builder.Services.AddSingleton<ICacheManager, CacheManager>();

var app = builder.Build();

app.RegisterCDNEndpoints();

app.Run();
