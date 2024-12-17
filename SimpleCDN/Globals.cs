/*
 * This file combines AssemblyInfo.cs and GlobalSuppressions.cs into a single file, as they are both used to configure the assembly.
 * In here, we also have the Globals class for constant Assembly-wide values.
 */

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// Assembly Info
[assembly: InternalsVisibleTo("SimpleCDN.Tests")]
[assembly: InternalsVisibleTo("SimpleCDN.Tests.Integration")]

// Global Suppressions
[assembly: SuppressMessage("Roslynator", "RCS1102:Make class static", Justification = "Needed for integration tests", Scope = "type", Target = "~T:SimpleCDN.Program")]

namespace SimpleCDN
{
	/// <summary>
	/// Constant values that are used throughout the application and should not change during runtime
	/// </summary>
	internal static class Globals
	{
		// NOTE: when changing this value, make sure to also change the references in wwwroot/index.html
		// and probably some other places (use the search feature of your IDE!)
		public const string SystemFilesRoot = "/_cdn";
	}
}
