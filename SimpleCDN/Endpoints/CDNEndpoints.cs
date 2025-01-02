using Microsoft.Net.Http.Headers;
using SimpleCDN.Helpers;
using SimpleCDN.Services;
using SimpleCDN.Services.Compression;

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
			// health check endpoint
			builder.MapGet(Globals.SystemFilesRoot + "/server/health", () => "healthy");

			builder.MapGet("/{*route}", (ICDNLoader loader, HttpContext ctx, ILogger<CDN> logger, ICompressionManager compressionManager, string route = "") =>
			{
				try
				{
					var acceptedEncodings = ctx.Request.Headers.AcceptEncoding.ToString().Split(',', StringSplitOptions.TrimEntries);

					var preferredAlgorithm = CompressionAlgorithm.MostPreferred(acceptedEncodings);

					if (loader.GetFile(route) is CDNFile file)
					{
						IList<MediaTypeHeaderValue> typedAccept = ctx.Request.GetTypedHeaders().Accept;
						// check if the client accepts the file's media type
						if (typedAccept.Count > 0 && !typedAccept.ContainsMediaType(file.MediaType))
						{
							return Results.StatusCode(StatusCodes.Status406NotAcceptable);
						}

						if (file is BigCDNFile bigFile) // file was too big to load into memory
						{
							return Results.File(bigFile.FilePath, bigFile.MediaType, lastModified: bigFile.LastModified, enableRangeProcessing: true);
						}

						byte[] bytes = file.Content;
						CompressionAlgorithm algorithm = file.Compression;

						if (algorithm != CompressionAlgorithm.None && !acceptedEncodings.Contains(algorithm.Name))
						{
							// client doesn't accept the file's compression algorithm
							bytes = compressionManager.Decompress(algorithm, bytes);
							algorithm = CompressionAlgorithm.None;
						}

						if (algorithm == CompressionAlgorithm.None && preferredAlgorithm != CompressionAlgorithm.None)
						{
							compressionManager.Compress(preferredAlgorithm, bytes, out int newLength);
							// client prefers a different compression algorithm
							if (newLength < bytes.Length)
							{
								// compression succeeded in-place, but we still need to trim the array
								bytes = bytes[..newLength];
							}

							algorithm = preferredAlgorithm;
						}

						// set the content encoding header
						if (algorithm != CompressionAlgorithm.None)
						{
							ctx.Response.Headers.ContentEncoding = algorithm.Name;
						}

						return Results.File(bytes, file.MediaType, enableRangeProcessing: true, lastModified: file.LastModified);
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
