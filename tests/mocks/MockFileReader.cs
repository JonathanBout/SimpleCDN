﻿using SimpleCDN.Helpers;
using SimpleCDN.Services;

namespace SimpleCDN.Tests.Mocks
{
	public record MockFile(DateTimeOffset LastModified, byte[] Content);

	public class MockFileReader(Dictionary<string, MockFile> files) : IPhysicalFileReader, ISystemFileReader
	{
		// small threshold for easier testing
		const int ARRAY_SIZE_THRESHOLD = 1000;

		private readonly Dictionary<string, MockFile> _files = files;

		public bool CanLoadIntoArray(string path)
		{
			if (_files.TryGetValue(path, out MockFile? file))
				return file.Content.Length <= ARRAY_SIZE_THRESHOLD;

			return false;
		}

		public bool CanLoadIntoArray(long size) => size <= ARRAY_SIZE_THRESHOLD;

		public bool DirectoryExists(string path)
		{
			// name should start with path and have at least one more character
			return _files.Keys.Any(f => f.StartsWith(path) && !f.Equals(path));
		}

		public bool FileExists(string path) => _files.ContainsKey(path);

		public IEnumerable<FileSystemInfo> GetEntries(string path)
		{
			return _files.Where(f => f.Key.StartsWith(path)).Select(f => new MockFileSystemInfo(f.Key));
		}

		public IEnumerable<string> GetFiles(string path, string pattern = "*")
		{
			return _files.Keys.Where(f => f.StartsWith(path));
		}

		public DateTimeOffset GetLastModified(string path) => GetFile(path)?.LastModified ?? DateTimeOffset.MinValue;
		public long GetSize(string path) => GetFile(path)?.Content.LongLength ?? long.MaxValue;

		public CDNFile? GetSystemFile(ReadOnlySpan<char> path, IEnumerable<CompressionAlgorithm> acceptedCompression)
		{
			var fullPath = path.ToString();
			if (_files.TryGetValue(fullPath, out MockFile? file))
				return new CDNFile(file.Content, MimeTypeHelpers.MimeTypeFromFileName(fullPath).ToContentTypeString(), file.LastModified, CompressionAlgorithm.None);
			return null;
		}

		public bool IsDotFile(string path)
		{
			var sections = path.Split('/', '\\');
			return sections.Any(s => s.StartsWith('.'));
		}

		public byte[] LoadIntoArray(string path) => GetFile(path) is MockFile f ? f.Content : [];
		public Stream OpenFile(string path) => GetFile(path) is MockFile f ? new MemoryStream(f.Content) : Stream.Null;

		private MockFile? GetFile(string path)
		{
			if (_files.TryGetValue(path, out MockFile? file))
				return file;
			return null;
		}

		private class MockFileSystemInfo(string fullName) : FileSystemInfo
		{
			public override bool Exists => true;

			public override string Name => Path.GetFileName(fullName);

			public override string FullName => fullName;

			public override void Delete() { }
		}
	}
}
