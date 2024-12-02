using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System;
using System.IO.Compression;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleCDN.Services
{
	public record CDNFile(byte[] Content, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression);

	public record BigCDNFile(string FilePath, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression) : CDNFile([], MediaType, LastModified, Compression);

	public partial class CDNLoader(
		IWebHostEnvironment environment,
		IOptionsMonitor<CDNConfiguration> options,
		IIndexGenerator generator,
		ILogger<CDNLoader> logger,
		ICacheManager cache,
		IPhysicalFileReader fs) : ICDNLoader
	{
		private readonly IWebHostEnvironment _environment = environment;
		private readonly IIndexGenerator _indexGenerator = generator;
		private readonly ILogger<CDNLoader> _logger = logger;
		private readonly IPhysicalFileReader _fs = fs;
		private readonly ICacheManager _cache = cache;

		private readonly IOptionsMonitor<CDNConfiguration> _options = options;

		string DataRoot => _options.CurrentValue.DataRoot;

		public CDNFile? GetFile(string path)
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
			var pathSpan = pathChars.AsSpan();

			pathSpan.Normalize();

			path = pathSpan.ToString();

			//if (!pathSpan.StartsWith(DataRoot.AsSpan()))
			//{
			//	_logger.LogWarning("Path '{path}' refers to a file outside of the data root '{DataRoot}'", path.ForLog(), DataRoot.ForLog());
			//	return null;
			//}

			if (pathSpan.StartsWith("/_cdn/"))
			{
				return GetSystemFile(path[5..]);
			}

			var filesystemPath = Path.Combine(DataRoot, path);

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

		private CDNFile? GetSystemFile(string requestPath)
		{
			var fileInfo = _environment.WebRootFileProvider.GetFileInfo(requestPath);

			if (!fileInfo.Exists || fileInfo.PhysicalPath is null || fileInfo.IsDirectory)
			{
				_cache.TryRemove(requestPath);
				return null;
			}

			if (_cache.TryGetValue(requestPath, out var cachedFile) && cachedFile.LastModified >= fileInfo.LastModified)
			{
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			CompressionAlgorithm compression = CompressionAlgorithm.None;
			long originalLength = fileInfo.Length;

			if (_environment.WebRootFileProvider.GetFileInfo(requestPath + ".gz") is { Exists: true, IsDirectory: false, PhysicalPath: not null } compressedFile)
			{
				compression = CompressionAlgorithm.GZip;
				fileInfo = compressedFile;
			}

			// if the file is too big to load into memory, we want to stream it directly 
			if (!_fs.CanLoadIntoArray(fileInfo.Length))
			{
				return new BigCDNFile(fileInfo.PhysicalPath, MimeTypeHelpers.MimeTypeFromFileName(requestPath).ToContentTypeString(), fileInfo.LastModified, CompressionAlgorithm.None);
			}

			var content = _fs.LoadIntoArray(fileInfo.PhysicalPath);

			var lastModified = fileInfo.LastModified;

			var mediaType = MimeTypeHelpers.MimeTypeFromFileName(requestPath);

			// if no compressed equivalent existed on disk, try to compress it ourselves.
			if (compression == CompressionAlgorithm.None)
			{
				var contentSpan = content.AsSpan();
				if (GZipHelpers.TryCompress(ref contentSpan))
				{
					compression = CompressionAlgorithm.GZip;
					content = contentSpan.ToArray();
				}
			}

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

			if (_cache.TryGetValue(requestPath, out var cachedFile) && cachedFile.LastModified > _fs.GetLastModified(absolutePath))
			{
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			var index = _indexGenerator.GenerateIndex(absolutePath, requestPath);

			if (index is null)
			{
				return null;
			}

			var indexSpan = index.AsSpan();
			var indexCompresion = CompressionAlgorithm.None;
			var originalSize = indexSpan.Length;

			if (GZipHelpers.TryCompress(ref indexSpan))
			{
				indexCompresion = CompressionAlgorithm.GZip;
				index = indexSpan.ToArray();
			}

			_cache.CacheFile(requestPath, index, originalSize, _fs.GetLastModified(absolutePath), MimeType.HTML, indexCompresion);

			return new CDNFile(index, MimeType.HTML.ToContentTypeString(), _fs.GetLastModified(absolutePath), indexCompresion);
		}

		private CDNFile? GetRegularFile(string absolutePath, string requestPath)
		{
			if (!_fs.FileExists(absolutePath))
			{
				return null;
			}

			var actualLastModified = _fs.GetLastModified(absolutePath);

			if (_cache.TryGetValue(requestPath, out var cachedFile) && cachedFile.LastModified > actualLastModified)
			{
				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
			}

			if (!_fs.CanLoadIntoArray(absolutePath))
			{
				return new BigCDNFile(absolutePath, MimeTypeHelpers.MimeTypeFromFileName(requestPath).ToContentTypeString(), actualLastModified, CompressionAlgorithm.None);
			}

			var content = _fs.LoadIntoArray(absolutePath);

			var mediaType = MimeTypeHelpers.MimeTypeFromFileName(requestPath);

			var compression = CompressionAlgorithm.None;

			var contentSpan = content.AsSpan();

			var uncompressedLength = contentSpan.Length;

			if (GZipHelpers.TryCompress(ref contentSpan))
			{
				compression = CompressionAlgorithm.GZip;
				content = contentSpan.ToArray();
			}

			_cache.CacheFile(requestPath, content, uncompressedLength, actualLastModified, mediaType, compression);

			return new CDNFile(content, mediaType.ToContentTypeString(), actualLastModified, compression);
		}
	}
}
