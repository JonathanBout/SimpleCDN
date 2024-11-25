using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Text;

namespace SimpleCDN
{
	public record CDNFile(byte[] Content, string MediaType, DateTimeOffset LastModified);
	public class CDNLoader(IWebHostEnvironment environment, IOptionsMonitor<CDNConfiguration> options)
	{
		private readonly IWebHostEnvironment _environment = environment;

		private readonly Dictionary<string, CachedFile> _cache = new(StringComparer.OrdinalIgnoreCase);

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

				return new CDNFile(cachedFile.Content, cachedFile.MimeType.ToContentTypeString(), cachedFile.LastModified);
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
				return new CDNFile(cached.Content, cached.MimeType.ToContentTypeString(), cached.LastModified);
			}

			if (!info.Exists)
			{
				return null;
			}

			using var stream = info.CreateReadStream();
			var bytes = new byte[stream.Length];
			stream.ReadExactly(bytes);

			var mime = MimeTypeHelpers.MimeTypeFromFileName(info.PhysicalPath);

			_cache[info.PhysicalPath] = new CachedFile
			{
				Content = bytes,
				LastModified = info.LastModified,
				MimeType = mime
			};

			return new CDNFile(bytes, mime.ToContentTypeString(), info.LastModified);
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
						LastModified = lastModified,
						MimeType = MimeType.HTML
					};
				}

			} else
			{
				lastModified = new DateTimeOffset(File.GetLastWriteTimeUtc(absolutePath));
			}

			if (content.content is null) return null;

			_cache[absolutePath] = file ??= new CachedFile
			{
				Content = content.content,
				LastModified = lastModified,
				MimeType = content.type
			};

			return new CDNFile(file.Content, file.MimeType.ToContentTypeString(), file.LastModified);
		}


		private static (MimeType type, byte[]? content) TryLoadIndex(string absolutePath, string rootRelativePath)
		{
			if (!Directory.Exists(absolutePath)) return MimeTypeHelpers.Empty;

			var indexes = Directory.EnumerateFiles(absolutePath, "index.htm?");

			foreach (var indexFile in indexes)
			{
				var substring = indexFile.AsSpan()[indexFile.LastIndexOf('.')..];

				if (substring == "html" || substring == "htm")
				{
					var loaded = LoadFileFromDisk(indexFile);

					// check after loading to make sure the file wasn't deleted in the mean time
					if (loaded is (MimeType, byte[])) return loaded;
				}
			}

			return (MimeType.HTML, GenerateIndex(absolutePath, rootRelativePath));
		}

		private static byte[]? GenerateIndex(string absolutePath, string rootRelativePath)
		{
			if (!Directory.Exists(absolutePath))
			{
				return null;
			}

			var directory = new DirectoryInfo(absolutePath);

			var index = new StringBuilder();

			index.AppendFormat(
				"""
				<html>
				<head>
					<meta name="robots" content="noindex,nofollow">
					<link rel="stylesheet" href="/_cdn/styles.css">
					<meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0, minimum-scale=1.0">
				</head>
				<body>
					<h1>Index of {0}</h1>
					<table>
						<thead><tr>
							<th>Name</th>
							<th>Size</th>
							<th>Last Modified</th>
						</tr></thead>
						<tbody>
				""", rootRelativePath);

			if (rootRelativePath is not "/" and not "" && directory.Parent is DirectoryInfo parent)
			{
				AppendRow(index, "..", "Parent Directory", -1, parent.LastWriteTimeUtc);
			}

			foreach (var subDirectory in directory.EnumerateDirectories())
			{
				var name = subDirectory.Name;

				AppendRow(index, Path.Combine(rootRelativePath, name), name, -1, subDirectory.LastWriteTimeUtc);
			}

			foreach (var file in directory.EnumerateFiles())
			{
				var name = file.Name;

				AppendRow(index, Path.Combine(rootRelativePath, name), name, file.Length, file.LastWriteTimeUtc);
			}

			index.Append("</tbody></table></body></html>");

			var bytes = Encoding.UTF8.GetBytes(index.ToString());

			return bytes;
		}

		private static void AppendRow(StringBuilder index, string href, string name, long size, DateTimeOffset lastModified)
		{
			index.AppendFormat("""<tr><td><a href="{0}">{1}</a></td>""", href, name);
			index.AppendFormat("""<td>{0}</td>""", size < 0 ? "-" : size.FormatByteCount());
			index.AppendFormat("""<td>{0}</td></tr>""", lastModified);
		}

		private static (MimeType type, byte[]? content) LoadFileFromDisk(string absolutePath)
		{
			if (!Path.IsPathRooted(absolutePath))
				return MimeTypeHelpers.Empty;

			if (!File.Exists(absolutePath)) return MimeTypeHelpers.Empty;

			return (MimeTypeHelpers.MimeTypeFromFileName(absolutePath), File.ReadAllBytes(absolutePath));
		}

		private string? GetFullPath(string relativePath)
		{
			if (relativePath is null) return null;

			relativePath = relativePath.TrimStart('/');



			var combined = Path.Combine(DataRoot, relativePath);

			var resolved = Path.GetFullPath(combined);

			// if the path contained for example ../file, we obviously don't allow access
			if (!resolved.StartsWith(DataRoot))
			{
				return null;
			}

			return resolved;
		}
	}
}
