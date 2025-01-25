using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using SimpleCDN.Endpoints;
using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Caching.Implementations;
using System.Net.Mime;
using System.Text.Json.Serialization;

namespace SimpleCDN.Standalone
{
	public static class AdditionalEndpoints
	{
		public static WebApplication MapEndpoints(this WebApplication app)
		{
			app.MapSimpleCDN();
#if DEBUG
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
					GC.WaitForPendingFinalizers();
					GC.Collect();

					return Results.Ok();
				}
				return Results.NotFound();
			});

#endif
			app.MapHealthChecks();

			app.MapGet("/favicon.ico", () => Results.Redirect("/" + GlobalConstants.SystemFilesRelativePath + "/logo.ico", true));

			return app;
		}

		private static WebApplication MapHealthChecks(this WebApplication app)
		{
			app.MapHealthChecks("/" + GlobalConstants.SystemFilesRelativePath + "/server/health", new HealthCheckOptions
			{
				ResponseWriter = async (ctx, health) =>
				{
					JsonOptions jsonOptions = ctx.RequestServices.GetRequiredService<IOptionsSnapshot<JsonOptions>>().Value;
					ctx.Response.ContentType = MediaTypeNames.Application.Json;
#pragma warning disable IL2026, IL3050 // it thinks it requires unreferenced code,
					// but the TypeInfoResolverChain actually provides the necessary context
					await ctx.Response.WriteAsJsonAsync(health);
#pragma warning restore IL2026, IL3050
				}
			});

			return app;
		}
	}
}
