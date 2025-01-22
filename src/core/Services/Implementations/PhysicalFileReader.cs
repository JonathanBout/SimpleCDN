using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Implementations
{
	internal class PhysicalFileReader(ILogger<PhysicalFileReader> logger, IOptionsMonitor<CDNConfiguration> options) : IPhysicalFileReader
	{
		private readonly ILogger<PhysicalFileReader> _logger = logger;
		private readonly IOptionsMonitor<CDNConfiguration> _options = options;

		public byte[] LoadIntoArray(string path)
		{
			try
			{
				using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				var bytes = new byte[stream.Length];
				stream.ReadExactly(bytes);
				return bytes;
			} catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load file '{path}' into memory", path.ForLog());

				return [];
			}
		}

		public Stream OpenFile(string path)
		{
			try
			{
				return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			} catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to open file '{path}'", path.ForLog());
				return Stream.Null;
			}
		}

		public bool FileExists(string path)
		{
			return File.Exists(path);
		}

		public bool DirectoryExists(string path)
		{
			return Directory.Exists(path);
		}

		public IEnumerable<string> GetFiles(string path, string pattern = "*")
		{
			return Directory.EnumerateFiles(path, pattern);
		}
		public IEnumerable<FileSystemInfo> GetEntries(string path)
		{
			return new DirectoryInfo(path).EnumerateFileSystemInfos();
		}

		public DateTimeOffset GetLastModified(string path)
		{
			return File.GetLastWriteTimeUtc(path);
		}

		public bool IsDotFile(string path)
		{
			if (!FileExists(path) && !DirectoryExists(path))
				return false;

			ReadOnlySpan<char> pathSpan = path.AsSpan();

			pathSpan = pathSpan[_options.CurrentValue.DataRoot.Length..];

			foreach (Range section in pathSpan.SplitToPathSegments())
			{
				if (StartsWithDot(pathSpan[section.Start..section.End]))
					return true;
			}

			return false;
		}

		private static bool StartsWithDot(ReadOnlySpan<char> span)
		{
			return span.Length > 0 && span[0] == '.';
		}

		public long GetSize(string path)
		{
			try
			{
				return new FileInfo(path).Length;
			} catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to get size of file '{path}'", path.ForLog());
				return long.MaxValue;
			}
		}
	}
}
