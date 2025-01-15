using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using SimpleCDN.Services;
using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Compression;

namespace SimpleCDN.Endpoints
{
	/// <summary>
	/// A dummy class for the logger type.
	/// </summary>
	internal class CDN;

	/// <summary>
	/// Contains extension methods for mapping SimpleCDN endpoints.
	/// </summary>
	public static class CDNEndpoints
	{
		/// <summary>
		/// Maps the SimpleCDN endpoints to the provided builder.
		/// </summary>
		public static IEndpointRouteBuilder MapSimpleCDN(this IEndpointRouteBuilder builder)
		{
#if DEBUG
			// cache debug view. Only available in debug builds, as it exposes internal cache data
			builder.MapGet(GlobalConstants.SystemFilesRelativePath + "/server/cache", (ICacheManager cache) => cache.GetDebugView());

#endif
			builder.MapGet($"{{*{GlobalConstants.CDNRouteValueKey}}}",
			([FromRoute(Name = GlobalConstants.CDNRouteValueKey)] string? route,
				ICDNLoader loader,
				HttpContext ctx,
				ILogger<CDN> logger,
				ICompressionManager compressionManager,
				ICDNContext cdnContext,
				IOptionsSnapshot<CDNConfiguration> options) =>
			{
				ctx.Response.Headers.Server = "SimpleCDN";
				if (options.Value.BlockRobots)
				{
					ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
				}

				if (route is null)
				{
					if (ctx.Request.Path.Value?.EndsWith('/') is true)
					{
						route = "/";
					} else
					{
						route = "";
					}
				}

				try
				{
					var acceptedEncodings = ctx.Request.Headers.AcceptEncoding.ToString().Split(',', StringSplitOptions.TrimEntries);

					var preferredAlgorithm = CompressionAlgorithm.MostPreferred(PerformancePreference.None, acceptedEncodings);

					if (loader.GetFile(route) is CDNFile file)
					{
						if (file is RedirectCDNFile redirect)
						{
							string basePath = ctx.Request.Path.ToString();

							if (route.Length > 0)
							{
								basePath = basePath.Replace(route, "", StringComparison.Ordinal);

								if (basePath.EndsWith('/'))
								{
									basePath = basePath[..^1];
								}
							}

							string fullDestination;
							if (redirect.Destination.StartsWith('/'))
							{
								fullDestination = basePath + redirect.Destination;
							} else
							{
								fullDestination = basePath + "/" + redirect.Destination;
							}
							return Results.Redirect(fullDestination, redirect.Permanent);
						}

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

						if (algorithm != CompressionAlgorithm.None && !acceptedEncodings.Contains(algorithm.HttpName))
						{
							// client doesn't accept the file's compression algorithm
							bytes = compressionManager.Decompress(algorithm, bytes);
							algorithm = CompressionAlgorithm.None;
						}

						if (algorithm == CompressionAlgorithm.None
							&& preferredAlgorithm != CompressionAlgorithm.None
							&& compressionManager.Compress(preferredAlgorithm, bytes, out int newLength))
						{
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
							ctx.Response.Headers.ContentEncoding = algorithm.HttpName;
						}

						return Results.File(bytes, file.MediaType, enableRangeProcessing: true, lastModified: file.LastModified);
					}
				} catch (Exception ex)
				{
					logger.LogError(ex, "Failed loading file or index at '{path}'", route.ForLog());
					return Results.InternalServerError();
				}

				return Results.NotFound();
			});

			return builder;
		}
	}
}
