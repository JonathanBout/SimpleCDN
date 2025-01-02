using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using SimpleCDN.Services.Compression;
using System.Net.Mime;

namespace SimpleCDN.Services
{
	public record CDNFile(byte[] Content, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression);

	public record BigCDNFile(string FilePath, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression) : CDNFile([], MediaType, LastModified, Compression);

	public class CDNLoader(
		IWebHostEnvironment environment,
		IOptionsMonitor<CDNConfiguration> options,
		IIndexGenerator generator,
		ICacheManager cache,
		ILogger<CDNLoader> logger,
		IPhysicalFileReader fs) : ICDNLoader
	{
		private readonly IWebHostEnvironment _environment = environment;
		private readonly IIndexGenerator _indexGenerator = generator;
		private readonly IPhysicalFileReader _fs = fs;
		private readonly ICacheManager _cache = cache;
		private readonly ILogger<CDNLoader> _logger = logger;

		private readonly IOptionsMonitor<CDNConfiguration> _options = options;

		string DataRoot => _options.CurrentValue.DataRoot;

		public CDNFile? GetFile(string path, params IEnumerable<CompressionAlgorithm> acceptedCompression)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				path = "/";
			}

			if (!path.StartsWith('/'))
			{
				path = '/' + path;
			}

			var pathChars = path.ToCharArray();
			Span<char> pathSpan = pathChars.AsSpan();

			pathSpan.Normalize();

			path = pathSpan.ToString();

			if (pathSpan.StartsWith(Globals.SystemFilesRoot + "/") || path.Equals(Globals.SystemFilesRoot))
			{
				return GetSystemFile(path[5..], acceptedCompression);
			}

			var filesystemPath = Path.Combine(DataRoot, path.TrimStart('/'));

			if (!_options.CurrentValue.AllowDotFileAccess && _fs.IsDotFile(filesystemPath))
			{
				_logger.LogDebug("Denying access to dotfile or directory '{dotfile}'.", filesystemPath.ForLog());
				return null;
			}

			if (!_fs.FileExists(filesystemPath))
			{
				if (_fs.DirectoryExists(filesystemPath))
				{
					return GetIndexFile(filesystemPath, pathSpan.ToString());
				}

				return null;
			}

			return GetRegularFile(filesystemPath, path);
		}

		private CDNFile? GetSystemFile(string requestPath, params IEnumerable<CompressionAlgorithm> acceptedAlgorithms)
		{
			if (requestPath is "" or "/")
			{
				_logger.LogDebug("Redirecting '/' to system file 'index.html'");
				return GetSystemFile("index.html");
			}

			_logger.LogDebug("Requesting system file '{path}'", requestPath.ForLog());

			IFileInfo fileInfo = _environment.WebRootFileProvider.GetFileInfo(requestPath);

			if (!fileInfo.Exists || fileInfo.PhysicalPath is null || fileInfo.IsDirectory)
			{
				_cache.TryRemove(requestPath);

				_logger.LogDebug("System file '{path}' does not exist", requestPath);

				return null;
			}

			if (_cache.TryGetValue(requestPath, out CachedFile? cachedFile) && cachedFile.LastModified >= fileInfo.LastModified)
			{
				_logger.LogDebug("Serving system file '{path}' from cache", requestPath);
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			CompressionAlgorithm compression = CompressionAlgorithm.None;
			long originalLength = fileInfo.Length;

			var preferred = CompressionAlgorithm.MostPreferred(PerformancePreference.None, acceptedAlgorithms);

			if (preferred != CompressionAlgorithm.None
				&& _environment.WebRootFileProvider.GetFileInfo(requestPath + preferred.FileExtension)
					is { Exists: true, IsDirectory: false, PhysicalPath: not null } compressedFile)
			{
				fileInfo = compressedFile;
				compression = preferred;
			}

			// if the file is too big to load into memory, we want to stream it directly	
			if (!_fs.CanLoadIntoArray(fileInfo.Length))
			{
				_logger.LogDebug("System file '{path}' is too big to load into memory, streaming instead", requestPath.ForLog());
				return new BigCDNFile(fileInfo.PhysicalPath, MimeTypeHelpers.MimeTypeFromFileName(requestPath).ToContentTypeString(), fileInfo.LastModified, compression);
			}

			var content = _fs.LoadIntoArray(fileInfo.PhysicalPath);

			DateTimeOffset lastModified = fileInfo.LastModified;

			MimeType mediaType = MimeTypeHelpers.MimeTypeFromFileName(requestPath);

			// unchecked cast is safe because we know the file is small enough
			_cache.CacheFile(requestPath, content, unchecked((int)originalLength), lastModified, mediaType, compression);

			return new CDNFile(content, mediaType.ToContentTypeString(), lastModified, compression);
		}

		private CDNFile? GetIndexFile(string absolutePath, string requestPath)
		{
			if (!_fs.DirectoryExists(absolutePath))
			{
				return null;
			}

			var absoluteIndexHtml = Path.Combine(absolutePath, "index.html");

			if (_fs.FileExists(absoluteIndexHtml))
			{
				return GetRegularFile(absoluteIndexHtml, Path.Combine(requestPath, "index.html"));
			}

			if (_cache.TryGetValue(requestPath, out CachedFile? cachedFile) && cachedFile.LastModified > _fs.GetLastModified(absolutePath))
			{
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			var index = _indexGenerator.GenerateIndex(absolutePath, requestPath);

			if (index is null)
			{
				return null;
			}

			_cache.CacheFile(requestPath, index, index.Length, _fs.GetLastModified(absolutePath), MimeType.HTML, CompressionAlgorithm.None);

			return new CDNFile(index, MimeType.HTML.ToContentTypeString(), _fs.GetLastModified(absolutePath), CompressionAlgorithm.None);
		}

		private CDNFile? GetRegularFile(string absolutePath, string requestPath)
		{
			if (!_fs.FileExists(absolutePath))
			{
				return null;
			}

			if (_cache.TryGetValue(requestPath, out CachedFile? cachedFile) && cachedFile.LastModified > _fs.GetLastModified(absolutePath))
			{
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			if (!_fs.CanLoadIntoArray(absolutePath))
			{
				_logger.LogDebug("File '{path}' is too big to load into memory, streaming instead", requestPath.ForLog());
				return new BigCDNFile(absolutePath, MimeTypeHelpers.MimeTypeFromFileName(requestPath).ToContentTypeString(), _fs.GetLastModified(absolutePath), CompressionAlgorithm.None);
			}

			var content = _fs.LoadIntoArray(absolutePath);

			_cache.CacheFile(requestPath, content, content.Length, _fs.GetLastModified(absolutePath), MimeTypeHelpers.MimeTypeFromFileName(requestPath), CompressionAlgorithm.None);

			return new CDNFile(content, MimeTypeHelpers.MimeTypeFromFileName(requestPath).ToContentTypeString(), _fs.GetLastModified(absolutePath), CompressionAlgorithm.None);
		}
	}
}
