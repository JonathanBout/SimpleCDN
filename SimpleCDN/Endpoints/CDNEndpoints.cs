using Microsoft.Net.Http.Headers;
using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using SimpleCDN.Services;
using System.Linq;

namespace SimpleCDN.Endpoints
{
	/// <summary>
	/// A dummy class for the logger type
	/// </summary>
	public class CDN;
	public static class CDNEndpoints
	{
		public static IEndpointRouteBuilder RegisterCDNEndpoints(this IEndpointRouteBuilder builder)
		{
			builder.MapGet("/{*route}", (ICDNLoader loader, HttpContext ctx, ILogger<CDN> logger, string route = "") =>
			{
				try
				{
					if (loader.GetFile(route) is CDNFile file)
					{
						var typedAccept = ctx.Request.GetTypedHeaders().Accept;
						// check if the client accepts the file's media type
						if (typedAccept.Count > 0 && !typedAccept.ContainsMediaType(file.MediaType))
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
			});

			return builder;
		}
	}
}
