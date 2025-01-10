using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace SimpleCDN.Tests.Mocks
{
	public class MockFileProvider(string path) : IFileProvider
	{
		public IDirectoryContents GetDirectoryContents(string subpath)
		{
			return new MockDirectoryContents(Path.Combine(path, subpath));
		}
		public IFileInfo GetFileInfo(string subpath)
		{
			var subfolder = subpath[..subpath.LastIndexOf('/')];
			var filename = subpath[(subpath.LastIndexOf('/') + 1)..];

			return new MockFileInfo(Path.Combine(path, subfolder), filename);
		}
		public IChangeToken Watch(string filter)
		{
			return new MockChangeToken();
		}
	}
}
