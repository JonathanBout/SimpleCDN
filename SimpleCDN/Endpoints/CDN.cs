using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using SimpleCDN.Services;

namespace SimpleCDN.Endpoints
{
	public static class CDN
	{
		public static IEndpointRouteBuilder RegisterCDNEndpoints(this IEndpointRouteBuilder builder)
		{
			builder.MapGet("/{*route}", (CDNLoader loader, HttpContext ctx, ILogger<Program> Logger, string route = "") =>
			{
				if (loader.GetFile(route) is CDNFile file)
				{
					bool acceptsGzip = ctx.Request.Headers.AcceptEncoding.ToString().Split(',', StringSplitOptions.TrimEntries).Contains("gzip");

					byte[] bytes = file.Content;

					if (file.Compression == CompressionAlgorithm.GZip && acceptsGzip)
					{
						// append gzip to the list of accepted encodings
						ctx.Response.Headers.ContentEncoding = new(["gzip", .. ctx.Response.Headers.ContentEncoding.AsEnumerable()]);
					} else if (file.Compression == CompressionAlgorithm.GZip)
					{
						// file is compressed but client doesn't accept gzip
						// As basically all systems support gzip, this should rarely happen
						// and that's why we are ok with some performance loss here
						Logger.LogWarning("Client does not accept gzip encoding.");
						// decompress the file
						bytes = GZipHelpers.Decompress(bytes);
					}

					return Results.File(bytes, file.MediaType, lastModified: file.LastModified);
				}

				return Results.NotFound();
			}).CacheOutput(policy =>
			{
				// cache the response for 1 minute to reduce load on the server
				policy.Cache()
					.Expire(TimeSpan.FromMinutes(1));
			});

			return builder;
		}
	}
}
