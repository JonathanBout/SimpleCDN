using SimpleCDN;
using SimpleCDN.Configuration;
using System.IO.Compression;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOptions<CDNConfiguration>()
	.Configure<IConfiguration>((settings, configuration) =>
	{
		if (configuration["CDN_DATA_ROOT"] is string dataRoot)
		{
			settings.DataRoot = dataRoot;
		}

		if (configuration["CDN_CACHE_LIMIT"] is string maxMemoryString
			&& uint.TryParse(maxMemoryString, out uint maxMemory))
		{
			settings.MaxMemoryCacheSize = maxMemory;
		}
	})
	.BindConfiguration("CDN");

builder.Services.AddSingleton<CDNLoader>();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.MapGet("/{*route}", (CDNLoader loader, HttpContext ctx, string route = "") =>
{
	if (loader.GetFile(route) is CDNFile file)
	{
		if (file.IsCompressed)
		{
			ctx.Response.Headers.ContentEncoding = new(["gzip", .. ctx.Response.Headers.ContentEncoding.AsEnumerable()]);
		}
		return Results.File(file.Content, file.MediaType, lastModified: file.LastModified);
	}

	return Results.NotFound();
}).CacheOutput(policy =>
{
	policy.Cache()
		.Expire(TimeSpan.FromMinutes(1));
});

app.Run();
