using Microsoft.Extensions.FileProviders;
using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using SimpleCDN.Services.Caching;

namespace SimpleCDN.Services.Implementations
{
	internal class SystemFileReader(ILogger<SystemFileReader> logger, ICacheManager cache) : ISystemFileReader
	{
		const string SystemFilesNamespace = "SimpleCDN.SystemFiles";
		const string CacheKeyPrefix = "/" + GlobalConstants.SystemFilesRelativePath + "::";

		private readonly ILogger<SystemFileReader> _logger = logger;
		private readonly ICacheManager _cache = cache;

		private readonly EmbeddedFileProvider _systemFilesProvider = new(typeof(SystemFileReader).Assembly, SystemFilesNamespace);

		public CDNFile? GetSystemFile(ReadOnlySpan<char> requestPath, IEnumerable<CompressionAlgorithm> acceptedCompression)
		{
			var requestPathString = requestPath.TrimStart('/').ToString();

			var cacheKey = CacheKeyPrefix + requestPathString;

			_logger.LogDebug("Requesting system file '{path}'", requestPathString.ForLog());

			IFileInfo fileInfo = _systemFilesProvider.GetFileInfo(requestPathString);

			if (!fileInfo.Exists || fileInfo.IsDirectory)
			{
				_cache.TryRemove(cacheKey);

				_logger.LogDebug("System file '{path}' does not exist", requestPathString);

				return null;
			}

			if (_cache.TryGetValue(cacheKey, out CachedFile? cachedFile) && cachedFile.LastModified >= fileInfo.LastModified)
			{
				_logger.LogDebug("Serving system file '{path}' from cache", requestPathString);
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			CompressionAlgorithm compression = CompressionAlgorithm.None;
			var originalLength = fileInfo.Length;

			var preferred = CompressionAlgorithm.MostPreferred(PerformancePreference.None, acceptedCompression);

			if (preferred != CompressionAlgorithm.None
				&& _systemFilesProvider.GetFileInfo(requestPathString + preferred.FileExtension)
					is { Exists: true, IsDirectory: false } compressedFile)
			{
				fileInfo = compressedFile;
				compression = preferred;
			}

			MimeType mediaType = MimeTypeHelpers.MimeTypeFromFileName(requestPath);

			byte[] content;
			using (Stream contentStream = fileInfo.CreateReadStream())
			{
				content = new byte[contentStream.Length];
				contentStream.ReadExactly(content);
			}

			DateTimeOffset lastModified = fileInfo.LastModified;

			// unchecked cast is safe because we know the file is small enough
			_cache.CacheFile(cacheKey, content, unchecked((int)originalLength), lastModified, mediaType, compression);

			return new CDNFile(content, mediaType.ToContentTypeString(), lastModified, compression);
		}
	}
}
