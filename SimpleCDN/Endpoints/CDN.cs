using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using SimpleCDN.Services;

namespace SimpleCDN.Endpoints
{
	public static class CDN
	{
		public static IEndpointRouteBuilder RegisterCDNEndpoints(this IEndpointRouteBuilder builder)
		{
			builder.MapGet("/{*route}", (ICDNLoader loader, HttpContext ctx, ILogger<Program> logger, string route = "") =>
			{
				try
				{

					if (loader.GetFile(route) is CDNFile file)
					{
						// check if the client accepts the file's media type
						if (!ctx.Request.GetTypedHeaders().Accept.Contains(new MediaTypeHeaderValue(file.MediaType)))
						{
							return Results.StatusCode(StatusCodes.Status406NotAcceptable);
						}

						bool acceptsGzip = ctx.Request.Headers.AcceptEncoding.ToString().Split(',', StringSplitOptions.TrimEntries).Contains("gzip");

						if (file is BigCDNFile bigFile) // file was too big to load into memory
						{
							return Results.File(bigFile.FilePath, bigFile.MediaType, lastModified: bigFile.LastModified, enableRangeProcessing: true);
						}

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
							logger.LogWarning("Client does not accept gzip encoding.");
							// decompress the file
							bytes = GZipHelpers.Decompress(bytes);
						}

						return Results.File(bytes, file.MediaType, lastModified: file.LastModified, enableRangeProcessing: true);
					}
				} catch (Exception ex)
				{
					logger.LogError(ex, "Failed loading file or index at '{path}'", route.ForLog());
				}

				return Results.NotFound();
			}).CacheOutput(policy =>
			{
				// cache the response for 1 minute to reduce load on the server
				policy.Cache()
					.Expire(TimeSpan.FromMinutes(1))
					.SetVaryByRouteValue("route")
					.SetVaryByHeader("content-encoding");
			});

			return builder;
		}
	}
}
