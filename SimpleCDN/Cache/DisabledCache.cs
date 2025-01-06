using Microsoft.Extensions.Caching.Distributed;

namespace SimpleCDN.Cache
{
	/// <summary>
	/// A cache that does nothing. Used when caching is disabled.
	/// </summary>
	public class DisabledCache : IDistributedCache
	{
		public byte[]? Get(string key) => null;
		public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => FromResultOrCancelled(Get(key), token);
		public void Refresh(string key) { }
		public Task RefreshAsync(string key, CancellationToken token = default) => MayBeCancelled(token);
		public void Remove(string key) { }
		public Task RemoveAsync(string key, CancellationToken token = default) => MayBeCancelled(token);
		public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }
		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => MayBeCancelled(token);


		private static Task<T> FromResultOrCancelled<T>(T result, CancellationToken token)
		{
			return token.IsCancellationRequested ? Task.FromCanceled<T>(token) : Task.FromResult(result);
		}

		private static Task MayBeCancelled(CancellationToken token)
		{
			return token.IsCancellationRequested ? Task.FromCanceled(token) : Task.CompletedTask;
		}
	}
}
