using SimpleCDN;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOptions<CDNConfiguration>()
	.Configure<IConfiguration>((settings, configuration) =>
	{
		if (configuration["CDN_DATA_ROOT"] != null)
		{
			settings.DataRoot = configuration["CDN_DATA_ROOT"];
		}
	})
	.BindConfiguration("CDN");

builder.Services.AddSingleton<CDNLoader>();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.MapGet("/{*route}", (CDNLoader loader, string route = "") =>
{
	if (loader.GetFile(route) is CDNFile file)
	{
		return Results.File(file.Content, file.MediaType, lastModified: file.LastModified);
	}

	return Results.NotFound();
}).CacheOutput(policy =>
{
	policy.Cache()
		.Expire(TimeSpan.FromMinutes(1));
});

app.Run();
