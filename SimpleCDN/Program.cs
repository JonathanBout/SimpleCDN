using SimpleCDN;
using SimpleCDN.Configuration;
using SimpleCDN.Endpoints;
using SimpleCDN.Helpers;
using System.IO.Compression;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.Sources.Clear();

builder.Configuration
	.AddEnvironmentVariables()
	.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
	.AddCommandLine(args);

builder.Services.MapConfiguration();

builder.Services.AddSingleton<CDNLoader>();
builder.Services.AddSingleton<IndexGenerator>();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.RegisterCDNEndpoints();

app.Run();
