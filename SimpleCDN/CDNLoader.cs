using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.IO.Compression;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace SimpleCDN
{
	public record CDNFile(byte[] Content, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression);

	public record BigCDNFile(string FilePath, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression) : CDNFile([], MediaType, LastModified, Compression);

	public class CDNLoader(IWebHostEnvironment environment, IOptionsMonitor<CDNConfiguration> options, IndexGenerator generator, ILogger<CDNLoader> logger)
	{
		private readonly IWebHostEnvironment _environment = environment;
		private readonly IndexGenerator _indexGenerator = generator;
		private readonly ILogger<CDNLoader> _logger = logger;

		private readonly SizeLimitedCache _cache = new(options.CurrentValue.MaxMemoryCacheSize * 1000, StringComparer.OrdinalIgnoreCase);

		private readonly IOptionsMonitor<CDNConfiguration> _options = options;

		string DataRoot => _options.CurrentValue.DataRoot;

		public CDNFile? GetFile(string path)
		{
			// separate logic for cdn files
			if (path.StartsWith("_cdn/"))
			{
				return LoadCDNFile(path[5..]); // remove _cdn/ from path
			}

			// load favicon from logo.ico file in the CDN folder
			if (path.Contains("favicon.ico"))
				return LoadCDNFile("logo.ico");

			var fullPath = GetFullPath(path);

			// if the path is null, it points to a directory outside of the data root
			// which we for security reasons don't allow
			if (fullPath is null) return null;

			// if the file is in the cache, return it
			if (_cache.TryGetValue(fullPath, out var cachedFile))
			{
				// if the stored file is older than the one on disk, reload it
				if (File.GetLastWriteTimeUtc(fullPath) <= cachedFile.LastModified)
				{
					return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.Compression);
				}
			}

			return LoadFile(fullPath, "/" + path);
		}

		private CDNFile? LoadCDNFile(string path)
		{
			var file = _environment.WebRootFileProvider.GetFileInfo(path);

			// don't generate indexes for CDN folder
			if (file.IsDirectory || file.PhysicalPath is null)
			{ 
				return null;
			}

			if (_cache.TryGetValue(file.PhysicalPath, out CachedFile? cached) && cached is not null && cached.LastModified >= file.LastModified)
			{ 
				return new CDNFile(cached.Content, cached.MimeType.ToContentTypeString(), cached.LastModified, cached.Compression);
			}

			if (!file.Exists)
				return null;

			var mime = MimeTypeHelpers.MimeTypeFromFileName(file.PhysicalPath);

			var preCompressedPath = path + ".gz";

			CompressionAlgorithm compression = CompressionAlgorithm.None;
			long realSize = file.Length;
			string realPath = file.PhysicalPath;

			if (_environment.WebRootFileProvider.GetFileInfo(preCompressedPath) is { Exists: true, IsDirectory: false, PhysicalPath: not null } compressedFileInfo
				&& compressedFileInfo.LastModified >= file.LastModified
				&& compressedFileInfo.Length < file.Length)
			{
				compression = CompressionAlgorithm.GZip;
				file = compressedFileInfo;
			}

			// TODO: instead check with the cache provider if the size is supported
			if (file.Length > Array.MaxLength)
			{
				_logger.LogWarning("File {path} is too large to be cached or loaded into memory. Serving directly.", realPath.ReplaceLineEndings(""));
				return new BigCDNFile(file.PhysicalPath, mime.ToContentTypeString(), file.LastModified, compression);
			}

			using var stream = file.CreateReadStream();
			var bytes = new byte[stream.Length];
			stream.ReadExactly(bytes);

			_cache[realPath] = new CachedFile
			{
				Content = bytes,
				LastModified = file.LastModified,
				MimeType = mime,
				Size = (int)realSize,
				Compression = compression
			};

			return new CDNFile(bytes, mime.ToContentTypeString(), file.LastModified, CompressionAlgorithm.None);
		}

		private CDNFile? LoadFile(string absolutePath, string rootRelativePath)
		{
			var content = LoadFileFromDisk(absolutePath);

			CachedFile? file = null;

			// if the path is not a file, try to load an index file
			if (content.content is null)
			{
				content = TryLoadIndex(absolutePath, rootRelativePath);

				// if the content is a byte array, we generated an index
				if (content.content is byte[] bytes)
				{
					file = new CachedFile
					{
						Content = bytes,
						MimeType = MimeType.HTML,
						Size = content.content.Length,
					};
				}
			}

			// if the file is null, we couldn't load it from disk or generate an index
			if (content.content is null) return null;

			file ??= new CachedFile
			{
				Content = content.content,
				MimeType = content.type,
				Size = content.content.Length
			};

			file.LastModified = new DateTimeOffset(File.GetLastWriteTimeUtc(absolutePath));

			// attempt to compress the file if it's not already compressed
			if (file.Compression == CompressionAlgorithm.None)
			{
				var contentSpan = content.content.AsSpan();
				if (GZipHelpers.TryCompress(ref contentSpan))
				{
					file.Content = contentSpan.ToArray();
					file.Compression = CompressionAlgorithm.GZip;
				}
			}

			_cache[absolutePath] = file;

			return new CDNFile(file.Content, file.MimeType.ToContentTypeString(), file.LastModified, file.Compression);
		}

		/// <summary>
		/// Attempts to load an index file from the directory at <paramref name="absolutePath"/>. If no index file is found, generates one.
		/// </summary>
		/// <param name="absolutePath">The absolute path to the directory</param>
		/// <param name="rootRelativePath">The path to the directory, how it was requested</param>
		/// <returns></returns>
		private (MimeType type, byte[]? content) TryLoadIndex(string absolutePath, string rootRelativePath)
		{
			if (!Directory.Exists(absolutePath)) return MimeTypeHelpers.Empty;

			try
			{
				var indexes = Directory.EnumerateFiles(absolutePath, "index.htm?");

				var htmlPath = Path.Combine(absolutePath, "index.html");

				if (File.Exists(htmlPath))
				{
					var loaded = LoadFileFromDisk(htmlPath);
					if (loaded is (MimeType, byte[])) return loaded;
				}

				var htmPath = Path.Combine(absolutePath, "index.htm");

				if (File.Exists(htmPath))
				{
					var loaded = LoadFileFromDisk(htmPath);
					if (loaded is (MimeType, byte[])) return loaded;
				}

				return (MimeType.HTML, GenerateIndex(absolutePath, rootRelativePath));
			} catch (Exception ex)
				when (ex is UnauthorizedAccessException or SecurityException)
			{
				// if we can't access the directory, we can't generate an index
				_logger.LogError(ex, "Access denied to a publicly available folder ({path})", absolutePath.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", ""));
			} catch (Exception ex)
			{
				_logger.LogError(ex, "Error while trying to load index file for {path}", absolutePath.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", ""));
			}
			return MimeTypeHelpers.Empty;
		}

		private byte[]? GenerateIndex(string absolutePath, string rootRelativePath) => _indexGenerator.GenerateIndex(absolutePath, rootRelativePath);

		private static (MimeType type, byte[]? content) LoadFileFromDisk(string absolutePath)
		{
			if (!Path.IsPathRooted(absolutePath)) return MimeTypeHelpers.Empty;

			if (!File.Exists(absolutePath)) return MimeTypeHelpers.Empty;

			return (MimeTypeHelpers.MimeTypeFromFileName(absolutePath), File.ReadAllBytes(absolutePath));
		}

		private string? GetFullPath(string relativePath)
		{
			if (relativePath is null) return null;

			relativePath = relativePath.TrimStart('/');

			var combined = Path.Combine(DataRoot, relativePath);

			var resolved = Path.GetFullPath(combined);

			// if the path contained for example ../file and it resolves to a parent or sibling directory
			// of the data root, we obviously don't allow access
			if (!resolved.StartsWith(DataRoot))
			{
				return null;
			}

			return resolved;
		}
	}
}
