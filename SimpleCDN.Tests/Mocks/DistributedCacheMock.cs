using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Tests.Mocks
{
	internal class DistributedCacheMock : IDistributedCache
	{
		public Dictionary<string, byte[]> Values { get; set; } = [];

		public byte[]? Get(string key) => Values[key];
		public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
		{
			if (token.IsCancellationRequested) return Task.FromCanceled<byte[]?>(token);

			if (Values.TryGetValue(key, out var value))
			{
				return Task.FromResult<byte[]?>(value);
			}

			return Task.FromResult<byte[]?>(null);
		}

		public void Refresh(string key) { }

		public Task RefreshAsync(string key, CancellationToken token = default)
		{
			if (token.IsCancellationRequested) return Task.FromCanceled(token);

			Refresh(key);

			return Task.CompletedTask;
		}

		public void Remove(string key) => Values.Remove(key);
		public Task RemoveAsync(string key, CancellationToken token = default)
		{
			if (token.IsCancellationRequested) return Task.FromCanceled(token);

			Remove(key);

			return Task.CompletedTask;
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			Values[key] = value;
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			if (token.IsCancellationRequested) return Task.FromCanceled(token);
			Set(key, value, options);
			return Task.CompletedTask;
		}
	}
}
