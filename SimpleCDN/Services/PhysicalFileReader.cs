using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;

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
			return size <= _options.CurrentValue.MaxCachedItemSize;
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
				_logger.LogError(ex, "Failed to load file '{path}' into memory", path.ReplaceLineEndings(""));

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
				_logger.LogError(ex, "Failed to open file '{path}'", path.ReplaceLineEndings(""));
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

	}
}
