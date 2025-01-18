using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Caching.Implementations;

namespace SimpleCDN.Standalone
{
	public static class AdditionalEndpoints
	{
		public static WebApplication MapAdditionalEndpoints(this WebApplication app)
		{
#if DEBUG
			if (app.Configuration.GetSection("Cache:Type").Get<CacheType>() == CacheType.InMemory)
			{
				app.MapGet("/" + GlobalConstants.SystemFilesRelativePath + "/server/cache/clear", (HttpContext ctx, ICacheImplementationResolver cacheResolver) =>
				{
					ctx.Response.Headers.Clear();

					if (cacheResolver.Implementation is InMemoryCache imc)
					{
						imc.Clear();
						GC.Collect();
						return Results.Ok();
					}
					return Results.NotFound();
				});
			}
#endif
			// health check endpoint
			app.MapGet("/" + GlobalConstants.SystemFilesRelativePath + "/server/health", () => "healthy");

			app.MapGet("/favicon.ico", () => Results.Redirect("/" + GlobalConstants.SystemFilesRelativePath + "/logo.ico", true));

			return app;
		}
	}
}
