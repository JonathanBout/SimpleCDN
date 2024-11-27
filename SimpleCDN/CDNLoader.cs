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
using System.Text;

namespace SimpleCDN
{
	public record CDNFile(byte[] Content, string MediaType, DateTimeOffset LastModified, bool IsCompressed);
	public class CDNLoader(IWebHostEnvironment environment, IOptionsMonitor<CDNConfiguration> options, IndexGenerator generator)
	{
		private readonly IWebHostEnvironment _environment = environment;
		private readonly IndexGenerator _indexGenerator = generator;

		private readonly SizeLimitedCache _cache = new(options.CurrentValue.MaxMemoryCacheSize * 1000, StringComparer.OrdinalIgnoreCase);

		private readonly IOptionsMonitor<CDNConfiguration> _options = options;

		string DataRoot => _options.CurrentValue.DataRoot ?? _environment.WebRootPath;

		public CDNFile? GetFile(string path)
		{
			// separate logic for cdn files

			if (path.StartsWith("_cdn"))
			{
				return LoadCDNFile(path[5..]); // remove _cdn/ from path
			}

			var fullPath = GetFullPath(path);

			if (fullPath is null) return null;

			if (_cache.TryGetValue(fullPath, out var cachedFile))
			{
				var lastWrittenTime = new DateTimeOffset(File.GetLastWriteTimeUtc(fullPath));

				// if the stored file is older than the one on disk, reload it
				if (lastWrittenTime > cachedFile.LastModified)
				{
					return LoadFile(fullPath, "/" + path);
				}

				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified, cachedFile.IsCompressed);
			}

			return LoadFile(fullPath, "/" + path);
		}

		private CDNFile? LoadCDNFile(string path)
		{
			var info = _environment.WebRootFileProvider.GetFileInfo(path);

			// don't generate indexes for CDN folder
			if (info.IsDirectory || info.PhysicalPath is null)
			{
				return null;
			}

			if (_cache.TryGetValue(info.PhysicalPath, out CachedFile? cached) && cached is not null && cached.LastModified >= info.LastModified)
			{
				return new CDNFile(cached.Content, cached.MimeType.ToContentTypeString(), cached.LastModified, cached.IsCompressed);
			}

			if (!info.Exists)
			{
				return null;
			}
			var mime = MimeTypeHelpers.MimeTypeFromFileName(info.PhysicalPath);

			var preCompressedPath = path + ".gz";

			if (_environment.WebRootFileProvider.GetFileInfo(preCompressedPath) is { Exists: true } compressedFileInfo)
			{
				// file is pre-compressed using gzip

				using var stream = compressedFileInfo.CreateReadStream();
				var bytes = new byte[stream.Length];
				stream.ReadExactly(bytes);

				_cache[info.PhysicalPath] = new CachedFile
				{
					Content = bytes,
					LastModified = info.LastModified,
					Size = info.Length, // still use the length and mime type from the original file, not the compressed one
					MimeType = mime, 
					IsCompressed = true
				};

				return new CDNFile(bytes, mime.ToContentTypeString(), info.LastModified, false);
			} else
			{
				using var stream = info.CreateReadStream();
				var bytes = new byte[stream.Length];
				stream.ReadExactly(bytes);

				_cache[info.PhysicalPath] = new CachedFile
				{
					Content = bytes,
					LastModified = info.LastModified,
					MimeType = mime,
					Size = info.Length,
					IsCompressed = false
				};

				return new CDNFile(bytes, mime.ToContentTypeString(), info.LastModified, false);
			}
		}

		private CDNFile? LoadFile(string absolutePath, string rootRelativePath)
		{
			var content = LoadFileFromDisk(absolutePath);

			DateTimeOffset lastModified;

			CachedFile? file = null;

			if (content.content is null)
			{
				content = TryLoadIndex(absolutePath, rootRelativePath);
				lastModified = DateTimeOffset.UtcNow;

				if (content.content is byte[] bytes)
				{
					file = new CachedIndexFile
					{
						Content = bytes,
						DirectoryName = absolutePath,
						MimeType = MimeType.HTML,
						Size = content.content.Length
					};
				}
			} else
			{
				lastModified = new DateTimeOffset(File.GetLastWriteTimeUtc(absolutePath));
			}

			if (content.content is null) return null;


			file ??= new CachedFile
			{
				Content = content.content,
				LastModified = lastModified,
				MimeType = content.type,
				Size = content.content.Length
			};

			// attempt to compress the file if it's not already compressed
			if (!file.IsCompressed)
			{
				var contentSpan = content.content.AsSpan();
				bool compressed = GZipHelpers.TryCompress(ref contentSpan);
				file.Content = contentSpan.ToArray();
				file.IsCompressed = compressed;
			}

			_cache[absolutePath] = file;

			return new CDNFile(file.Content, file.MimeType.ToContentTypeString(), file.LastModified, file.IsCompressed);
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

			var indexes = Directory.EnumerateFiles(absolutePath, "index.htm?");

			foreach (var indexFile in indexes)
			{
				// efficiently check if the file is an htm(l) file
				var substring = indexFile.AsSpan()[(indexFile.LastIndexOf('.') + 1)..]; // + 1 to skip the dot

				if (substring.SequenceEqual("html") || substring.SequenceEqual("htm"))
				{
					// if the file is an index file, load it
					var loaded = LoadFileFromDisk(indexFile);

					// check after loading to make sure the file wasn't deleted in the mean time
					if (loaded is (MimeType, byte[])) return loaded;
				}
			}

			return (MimeType.HTML, GenerateIndex(absolutePath, rootRelativePath));
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
