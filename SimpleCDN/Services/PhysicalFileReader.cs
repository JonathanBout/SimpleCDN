using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Data;

namespace SimpleCDN.Services
{
	public class PhysicalFileReader(ILogger<PhysicalFileReader> logger, IOptionsMonitor<CDNConfiguration> options) : IPhysicalFileReader
	{
		private readonly ILogger<PhysicalFileReader> _logger = logger;
		private readonly IOptionsMonitor<CDNConfiguration> _options = options;

		public bool CanLoadIntoArray(string path)
		{
			try
			{
				var file = new FileInfo(path);
				return file.Exists && CanLoadIntoArray(file.Length);
			} catch
			{
				return false;
			}
		}

		public bool CanLoadIntoArray(long size)
		{
			return size <= _options.CurrentValue.MaxCachedItemSize * 1000; // convert kB to B
		}

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
			{
				return false;
			}

			ReadOnlySpan<char> pathSpan = path.AsSpan();

			pathSpan = pathSpan[_options.CurrentValue.DataRoot.Length..];

			foreach (Range section in pathSpan.SplitAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{
				if (section.GetOffsetAndLength(pathSpan.Length) is not { Length: > 0, Offset: int offset})
				{
					continue;
				}

				if (pathSpan[offset..].StartsWith('.'))
				{
					return true;
				}
			}

			return false;
		}
	}
}
