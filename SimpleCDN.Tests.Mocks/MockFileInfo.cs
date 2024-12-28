using Microsoft.Extensions.FileProviders;

namespace SimpleCDN.Tests.Mocks
{
	public class MockFileInfo(string path, string name) : IFileInfo
	{
		public bool Exists => true;
		public long Length => 0;
		public string PhysicalPath => path;
		public string Name => name;
		public DateTimeOffset LastModified => DateTimeOffset.Now;
		public bool IsDirectory => false;
		public Stream CreateReadStream() => new MemoryStream();
	}
}
