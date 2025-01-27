namespace SimpleCDN.Services.Caching
{
#if DEBUG
	/// <summary>
	/// Provides debug information about the cache implementation.
	/// </summary>
	internal interface ICacheDebugInfoProvider
	{
		internal object GetDebugInfo();
	}
#endif
}
