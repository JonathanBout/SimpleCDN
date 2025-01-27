namespace SimpleCDN.Services.Caching.Implementations
{
	// this partial class contains the debug information for the InMemoryCache,
	// and is only compiled in DEBUG mode
#if DEBUG
	partial class InMemoryCache : ICacheDebugInfoProvider
	{
		internal void Clear()
		{
			_dictionary.Clear();
		}

		public object GetDebugInfo()
		{
			return new SizeLimitedCacheDebugView(Size, MaxSize, Count, [.. Keys], FillPercentage: (double)Size / MaxSize);
		}
	}

	internal record SizeLimitedCacheDebugView(long Size, long MaxSize, int Count, string[] Keys, double FillPercentage);
#endif
}
