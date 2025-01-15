/*
 * This file combines AssemblyInfo.cs and GlobalSuppressions.cs into a single file, as they are both used to configure the assembly.
 * In here, we also have the Globals class for constant Assembly-wide values.
 */

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
		/// <summary>
		/// The root URL for the system files, like the style sheet.
		/// </summary>
		public const string SystemFilesRelativePath = "_cdn";

		/// <summary>
		/// The key of the <see cref="HttpRequest.RouteValues"/> item that contains the CDN route.
		/// This is used by the <see cref="CDNContext"/> to determine the base URL the CDN is placed at.
		/// </summary>
		public const string CDNRouteValueKey = "cdnRoute";
	}

	[JsonSourceGenerationOptions]
	[JsonSerializable(typeof(SizeLimitedCacheDebugView))]
#if DEBUG
	[JsonSerializable(typeof(BasicDebugView))]
	[JsonSerializable(typeof(DebugView))]
	[JsonSerializable(typeof(DetailedDebugView))]
#endif
	internal partial class SourceGenerationContext : JsonSerializerContext;
}
