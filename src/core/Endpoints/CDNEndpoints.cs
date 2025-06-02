using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using SimpleCDN.Services;
using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Compression;
using System.Diagnostics;

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
		/// <returns>
		/// The <typeparamref name="T"/> after mapping the SimpleCDN endpoints.
		/// </returns>
		public static T MapSimpleCDN<T>(this T builder) where T : IEndpointRouteBuilder
		{
#if DEBUG
			// cache debug view. Only available in debug builds, as it exposes internal cache data
			builder.MapGet(GlobalConstants.SystemFilesRelativePath + "/server/cache", (ICacheManager cache) => cache.GetDebugView());

#endif
			builder.MapGet($"{{*{GlobalConstants.CDNRouteValueKey}}}",
			([FromRoute(Name = GlobalConstants.CDNRouteValueKey)] string? route,
				HttpContext ctx,
				ICDNLoader loader,
				ILogger<CDN> logger,
				ICompressionManager compressionManager,
				IOptionsSnapshot<CDNConfiguration> options) =>
			{
				ctx.Response.Headers.Server = "SimpleCDN";
				if (options.Value.BlockRobots)
				{
					ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
				}

				if (route is null)
				{
					if (ctx.Request.Path.Value?[^1] is '/')
					{
						route = "/";
					} else
					{
						route = "";
					}
				}

				try
				{
					var acceptedEncodings = ctx.Request.Headers.AcceptEncoding
						.OfType<string>()
						.ToArray();

					var preferredAlgorithm = CompressionAlgorithm.MostPreferred(PerformancePreference.None, acceptedEncodings);

					if (loader.GetFile(route) is CDNFile file)
					{
						if (file is RedirectCDNFile redirect)
						{
							return Redirect(redirect, ctx, route);
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
#if NET9_0_OR_GREATER
					return Results.InternalServerError();
#else
					return Results.StatusCode(StatusCodes.Status500InternalServerError);
#endif
				}

				return Results.NotFound();
			});

			return builder;
		}

		private static IResult Redirect(RedirectCDNFile redirect, HttpContext ctx, string route)
		{
			// because the application may be hosted at a subpath, we need to construct the full destination URL.
			// To find this root path, we can use the full request path, and remove the requested route from it.
			// If the route is empty, we can just use the request path as the base path.
			// If the route is not empty, we need to remove it from the end of the base path.
			// Then, we can concatenate the base path with the redirect destination.

			ReadOnlySpan<char> basePath = ctx.Request.Path.Value ?? ReadOnlySpan<char>.Empty;

			Debug.Assert(basePath.EndsWith(route, StringComparison.OrdinalIgnoreCase));

			if (route.Length > 0)
			{
				basePath = basePath[..^route.Length];

			}
			if (!basePath.IsEmpty && basePath[^1] is '/')
			{
				basePath = basePath[..^1];
			}

			string fullDestination;
			if (redirect.Destination.StartsWith('/'))
			{
				fullDestination = string.Concat(basePath, redirect.Destination);
			} else
			{
				fullDestination = string.Concat(basePath, ['/'], redirect.Destination);
			}
			return Results.Redirect(fullDestination, redirect.Permanent);
		}
	}
}
