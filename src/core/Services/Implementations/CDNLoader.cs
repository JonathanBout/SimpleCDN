using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using SimpleCDN.Services.Caching;
using System.IO;

namespace SimpleCDN.Services.Implementations
{
	internal class CDNLoader(
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
			// as paths in index files are relative, we need the path to end in /.
			// Thats why we redirect to the same path with a trailing slash if it's missing.
			if (string.IsNullOrWhiteSpace(path))
				return new RedirectCDNFile("/", true);

			// normalize the path to remove any leading or trailing slashes
			var pathChars = path.ToCharArray();
			Span<char> pathSpan = pathChars.AsSpan();

			pathSpan.Normalize();


			// if the pathSpan starts with the system files root, we want to serve a system file
			// if there are additional characters after the system files root, the first one must be a '/'
			// as it could be /_cdnthings which wouldn't contain system files but rather user files

			if (pathSpan.StartsWith(GlobalConstants.SystemFilesRelativePath)
				&& (pathSpan.Length == GlobalConstants.SystemFilesRelativePath.Length
					|| pathSpan[GlobalConstants.SystemFilesRelativePath.Length] == '/'))
			{
				return GetSystemFile(pathSpan[GlobalConstants.SystemFilesRelativePath.Length..], acceptedCompression);
			}

			var filesystemPath = Path.Join(DataRoot.AsSpan(), pathSpan);

			if (!_options.CurrentValue.AllowDotFileAccess && _fs.IsDotFile(filesystemPath))
			{
				_logger.LogDebug("Denying access to dotfile or directory '{dotfile}'.", filesystemPath.ForLog());
				return null;
			}

			// if the path is not a file, we attempt to serve an index file
			if (!_fs.FileExists(filesystemPath))
			{
				return GetIndexFile(filesystemPath, pathSpan);
			}

			return GetRegularFile(filesystemPath, pathSpan);
		}

		/// <summary>
		/// Gets a system file from the CDN. System files are files that are part of the CDN itself, such as icons or stylesheets.
		/// </summary>
		/// <param name="requestPath">The path of the file to load, relative to the CDN root.</param>
		/// <param name="acceptedAlgorithms">Compression algorithms the client accepts.</param>
		private CDNFile? GetSystemFile(ReadOnlySpan<char> requestPath, params IEnumerable<CompressionAlgorithm> acceptedAlgorithms)
		{
			if (requestPath is "" or "/")
			{
				_logger.LogDebug("Rewriting '/' to system file 'index.html'");
				return GetSystemFile("index.html");
			}

			var requestPathString = requestPath.ToString();

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
			var originalLength = fileInfo.Length;

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

		/// <summary>
		/// Gets an index file from the CDN. Index files are generated for directories that don't contain an index.html file.
		/// </summary>
		private CDNFile? GetIndexFile(string absolutePath, ReadOnlySpan<char> requestPath)
		{
			if (!_fs.DirectoryExists(absolutePath))
				return null;

			if (!requestPath.EndsWith('/'))
			{
				// require trailing slash for directories
				return new RedirectCDNFile($"{requestPath}/", true);
			}

			var absoluteIndexHtml = Path.Join(absolutePath, "index.html");

			if (_fs.FileExists(absoluteIndexHtml))
				return GetRegularFile(absoluteIndexHtml, Path.Join(requestPath, "index.html"));

			var requestPathString = requestPath.ToString();

			if (_cache.TryGetValue(requestPathString, out CachedFile? cachedFile) && cachedFile.LastModified > _fs.GetLastModified(absolutePath))
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);

			var index = _indexGenerator.GenerateIndex(absolutePath, requestPathString);

			if (index is null)
				return null;

			_cache.CacheFile(requestPathString, index, index.Length, _fs.GetLastModified(absolutePath), MimeType.HTML, CompressionAlgorithm.None);

			return new CDNFile(index, MimeType.HTML.ToContentTypeString(), _fs.GetLastModified(absolutePath), CompressionAlgorithm.None);
		}

		/// <summary>
		/// Gets a regular content file from the CDN.
		/// </summary>
		private CDNFile? GetRegularFile(string absolutePath, ReadOnlySpan<char> requestPath)
		{
			if (!_fs.FileExists(absolutePath))
				return null;

			var requestPathString = requestPath.ToString();

			if (_cache.TryGetValue(requestPathString, out CachedFile? cachedFile) && cachedFile.LastModified > _fs.GetLastModified(absolutePath))
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);

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
