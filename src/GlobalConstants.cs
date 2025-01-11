/*
 * This file combines AssemblyInfo.cs and GlobalSuppressions.cs into a single file, as they are both used to configure the assembly.
 * In here, we also have the Globals class for constant Assembly-wide values.
 */

using SimpleCDN.Services.Caching.Implementations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

// Assembly Info
[assembly: InternalsVisibleTo("SimpleCDN.Tests.Unit")]
[assembly: InternalsVisibleTo("SimpleCDN.Tests.Integration")]
[assembly: InternalsVisibleTo("SimpleCDN.Benchmarks")]

namespace SimpleCDN
{
	/// <summary>
	/// Constant values that are used throughout the application and should not change during runtime
	/// </summary>
	internal static class GlobalConstants
	{
		// NOTE: when changing this value, make sure to also change the references in wwwroot/index.html
		// and probably some other places (use the global search feature of your IDE!)
		/// <summary>
		/// The root URL for the system files, like the style sheet
		/// </summary>
		public const string SystemFilesRootedPath = "/" + SystemFilesRelativePath;
		public const string SystemFilesRelativePath = "_cdn";
	}

	[JsonSourceGenerationOptions]
	[JsonSerializable(typeof(SizeLimitedCacheDebugView))]
	[JsonSerializable(typeof(CustomRedisCacheServiceDebugView))]
#if DEBUG
	[JsonSerializable(typeof(BasicDebugView))]
	[JsonSerializable(typeof(DebugView))]
	[JsonSerializable(typeof(DetailedDebugView))]
#endif
	internal partial class SourceGenerationContext : JsonSerializerContext;
}
