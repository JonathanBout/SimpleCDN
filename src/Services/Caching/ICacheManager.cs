﻿using Microsoft.Extensions.Caching.Distributed;
using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Services.Caching
{
	/// <summary>
	/// Represents a cache manager that can be used to cache files, using the first configured <see cref="IDistributedCache"/>
	/// </summary>
	public interface ICacheManager
	{
		/// <summary>
		/// Caches a file with the given content, size, last modified date, MIME type, and compression algorithm.
		/// </summary>
		void CacheFile(string path, byte[] content, int realSize, DateTimeOffset lastModified, MimeType mimeType, CompressionAlgorithm compression);
		/// <summary>
		/// Caches a file with the given path and <see cref="CachedFile"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="file"></param>
		void CacheFile(string path, CachedFile file);
		/// <summary>
		/// Gets a file from the cache by its path.
		/// </summary>
		bool TryGetValue(string path, [NotNullWhen(true)] out CachedFile? value);
		/// <summary>
		/// Removes a file from the cache by its path.
		/// </summary>
		/// <returns><see langword="true"/> when the file was succesfully removed.</returns>
		bool TryRemove(string path);
		/// <summary>
		/// Removes a file from the cache by its path, and provides it in the <paramref name="value"/> parameter.
		/// </summary>
		/// <returns><see langword="true"/> when the file was succesfully removed.</returns>
		bool TryRemove(string path, [NotNullWhen(true)] out CachedFile? value);

#if DEBUG
		/// <summary>
		/// Gets a debug view of the cache.
		/// </summary>
		object GetDebugView();
#endif
	}
}
