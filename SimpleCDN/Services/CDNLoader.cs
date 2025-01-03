using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

			// if the pathSpan starts with the system files root, we want to serve a system file
			// if there are additional characters after the system files root, the first one must be a '/'
			// as it could be /_cdnthings which wouldn't contain system files but rather user files

			if (pathSpan.StartsWith(Globals.SystemFilesRoot)
				&& (pathSpan.Length == Globals.SystemFilesRoot.Length
					|| pathSpan[Globals.SystemFilesRoot.Length] == '/'))
			{
				return GetSystemFile(pathSpan[Globals.SystemFilesRoot.Length..], acceptedCompression);
			}

			// trim the leading / from pathSpan to make sure we don't get a double slash
			var filesystemPath = Path.Join(DataRoot.AsSpan(), pathSpan[1..]);

			if (!_options.CurrentValue.AllowDotFileAccess && _fs.IsDotFile(filesystemPath))
			{
				_logger.LogDebug("Denying access to dotfile or directory '{dotfile}'.", filesystemPath.ForLog());
				return null;
			}

			if (!_fs.FileExists(filesystemPath))
			{
				if (_fs.DirectoryExists(filesystemPath))
				{
					return GetIndexFile(filesystemPath, pathSpan);
				}

				return null;
			}

			return GetRegularFile(filesystemPath, pathSpan);
		}

		private CDNFile? GetSystemFile(ReadOnlySpan<char> requestPath, params IEnumerable<CompressionAlgorithm> acceptedAlgorithms)
		{
			if (requestPath is "" or "/")
			{
				_logger.LogDebug("Rewriting '/' to system file 'index.html'");
				return GetSystemFile("index.html");
			}

			string requestPathString = requestPath.ToString();

			_logger.LogDebug("Requesting system file '{path}'", requestPathString.ForLog());

			IFileInfo fileInfo = _environment.WebRootFileProvider.GetFileInfo(requestPathString);

			if (!fileInfo.Exists || fileInfo.PhysicalPath is null || fileInfo.IsDirectory)
			{
				_cache.TryRemove(requestPathString);

				_logger.LogDebug("System file '{path}' does not exist", requestPathString);

				return null;
			}

			if (_cache.TryGetValue(requestPathString, out CachedFile? cachedFile) && cachedFile.LastModified >= fileInfo.LastModified)
			{
				_logger.LogDebug("Serving system file '{path}' from cache", requestPathString);
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			CompressionAlgorithm compression = CompressionAlgorithm.None;
			long originalLength = fileInfo.Length;

			var preferred = CompressionAlgorithm.MostPreferred(PerformancePreference.None, acceptedAlgorithms);

			if (preferred != CompressionAlgorithm.None
				&& _environment.WebRootFileProvider.GetFileInfo(requestPathString + preferred.FileExtension)
					is { Exists: true, IsDirectory: false, PhysicalPath: not null } compressedFile)
			{
				fileInfo = compressedFile;
				compression = preferred;
			}

			MimeType mediaType = MimeTypeHelpers.MimeTypeFromFileName(requestPath);

			// if the file is too big to load into memory, we want to stream it directly	
			if (!_fs.CanLoadIntoArray(fileInfo.Length))
			{
				_logger.LogDebug("System file '{path}' is too big to load into memory, streaming instead", requestPath.ForLog());
				return new BigCDNFile(fileInfo.PhysicalPath, mediaType.ToContentTypeString(), fileInfo.LastModified, compression);
			}

			var content = _fs.LoadIntoArray(fileInfo.PhysicalPath);

			DateTimeOffset lastModified = fileInfo.LastModified;

			// unchecked cast is safe because we know the file is small enough
			_cache.CacheFile(requestPathString, content, unchecked((int)originalLength), lastModified, mediaType, compression);

			return new CDNFile(content, mediaType.ToContentTypeString(), lastModified, compression);
		}

		private CDNFile? GetIndexFile(string absolutePath, ReadOnlySpan<char> requestPath)
		{
			if (!_fs.DirectoryExists(absolutePath))
			{
				return null;
			}

			var absoluteIndexHtml = Path.Join(absolutePath, "index.html");

			if (_fs.FileExists(absoluteIndexHtml))
			{
				return GetRegularFile(absoluteIndexHtml, Path.Join(requestPath, "index.html"));
			}

			string requestPathString = requestPath.ToString();

			if (_cache.TryGetValue(requestPathString, out CachedFile? cachedFile) && cachedFile.LastModified > _fs.GetLastModified(absolutePath))
			{
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			var index = _indexGenerator.GenerateIndex(absolutePath, requestPathString);

			if (index is null)
			{
				return null;
			}

			_cache.CacheFile(requestPathString, index, index.Length, _fs.GetLastModified(absolutePath), MimeType.HTML, CompressionAlgorithm.None);

			return new CDNFile(index, MimeType.HTML.ToContentTypeString(), _fs.GetLastModified(absolutePath), CompressionAlgorithm.None);
		}

		private CDNFile? GetRegularFile(string absolutePath, ReadOnlySpan<char> requestPath)
		{
			if (!_fs.FileExists(absolutePath))
			{
				return null;
			}

			string requestPathString = requestPath.ToString();

			if (_cache.TryGetValue(requestPathString, out CachedFile? cachedFile) && cachedFile.LastModified > _fs.GetLastModified(absolutePath))
			{
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			MimeType mediaType = MimeTypeHelpers.MimeTypeFromFileName(requestPathString);

			if (!_fs.CanLoadIntoArray(absolutePath))
			{
				_logger.LogDebug("File '{path}' is too big to load into memory, streaming instead", requestPath.ForLog());
				return new BigCDNFile(absolutePath, mediaType.ToContentTypeString(), _fs.GetLastModified(absolutePath), CompressionAlgorithm.None);
			}

			var content = _fs.LoadIntoArray(absolutePath);

			_cache.CacheFile(requestPathString, content, content.Length, _fs.GetLastModified(absolutePath), mediaType, CompressionAlgorithm.None);

			return new CDNFile(content, mediaType.ToContentTypeString(), _fs.GetLastModified(absolutePath), CompressionAlgorithm.None);
		}
	}
}
