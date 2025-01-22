using SimpleCDN.Endpoints;
using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Caching.Implementations;

namespace SimpleCDN.Standalone
{
	public static class AdditionalEndpoints
	{
		public static WebApplication MapEndpoints(this WebApplication app)
		{
			app.MapSimpleCDN();

#if DEBUG
			if (app.Configuration.GetSection("Cache:Type").Get<CacheType>() == CacheType.InMemory)
			{
				// if the cache used is the in-memory implementation, add an endpoint to clear it
				// as there is no other way to clear it without restarting the server,
				// opposed to for example the Redis implementation which has the Redis CLI
				app.MapGet("/" + GlobalConstants.SystemFilesRelativePath + "/server/cache/clear", (ICacheImplementationResolver cacheResolver) =>
				{
					// TODO: currently, the browser makes a request to favicon.ico after the cache is cleared,
					// which means there is a new cache entry created for the favicon.ico file.
					// This is not a problem, but it would be nice to prevent this from happening.

					if (cacheResolver.Implementation is InMemoryCache imc)
					{
						imc.Clear();
						// force garbage collection to make sure all memory
						// used by the cached files is properly released
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
