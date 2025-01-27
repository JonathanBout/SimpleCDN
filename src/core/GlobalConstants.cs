/*
 * This file combines AssemblyInfo.cs and GlobalSuppressions.cs into a single file,
 * as they are both used to configure things for the whole assembly.
 * In here, we also have the Globals class for constant Assembly-wide values, and the SourceGenerationContext for JSON serialization.
 */

using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimpleCDN.Services.Caching.Implementations;
using SimpleCDN.Services.Implementations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

// Assembly Info
[assembly: InternalsVisibleTo("SimpleCDN.Tests.Unit")]
[assembly: InternalsVisibleTo("SimpleCDN.Tests.Mocks")]
[assembly: InternalsVisibleTo("SimpleCDN.Tests.Integration")]
[assembly: InternalsVisibleTo("SimpleCDN.Benchmarks")]
[assembly: InternalsVisibleTo("SimpleCDN.Standalone")]

namespace SimpleCDN
{
	/// <summary>
	/// Constant values that are used throughout the application and should not change during runtime.
	/// </summary>
	internal static class GlobalConstants
	{
		// NOTE: when changing this value, make sure to also change the references in
		// other places like the Dockerfile health check (use the global search feature of your IDE!)
		// but ultimately, this value should never change.
		// This value should not start nor end with a slash.
		/// <summary>
		/// The root URL for the system files, like the style sheet.
		/// </summary>
		public const string SystemFilesRelativePath = "_cdn";

		// This value should only contain letters, numbers, and dashes.
		// But if you need to change it, you're probably doing something wrong.
		/// <summary>
		/// The key of the <see cref="HttpRequest.RouteValues"/> item that contains the CDN route.
		/// This is used by the <see cref="CDNContext"/> to determine the base URL the CDN is placed at.
		/// </summary>
		public const string CDNRouteValueKey = "cdnRoute";
	}

	[JsonSourceGenerationOptions(Converters = [typeof(JsonStringEnumConverter<HealthStatus>)])]
#if DEBUG // only generate serializers for debug views in debug mode
	[JsonSerializable(typeof(SizeLimitedCacheDebugView))]
	[JsonSerializable(typeof(BasicDebugView))]
	[JsonSerializable(typeof(DebugView))]
	[JsonSerializable(typeof(DetailedDebugView))]
#endif
	[JsonSerializable(typeof(object))]
	internal partial class SourceGenerationContext : JsonSerializerContext;
}
