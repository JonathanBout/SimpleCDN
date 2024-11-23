using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Internal;
using Microsoft.Extensions.Primitives;
using System.Collections;

namespace SimpleCDN.Tests
{
	internal class MockWebHostEnvironment : IWebHostEnvironment
	{
		public string WebRootPath
		{
			get => "/data";
			set { }
		}
		public IFileProvider WebRootFileProvider
		{
			get => new MockFileProvider("/data");
			set { }
		}
		public string ApplicationName
		{
			get => "application";
			set { }
		}
		public IFileProvider ContentRootFileProvider
		{
			get => new MockFileProvider("/data");
			set { }
		}
		public string ContentRootPath
		{
			get => "/data";
			set { }
		}

		public string EnvironmentName
		{
			get => "Test";
			set { }
		}
	}

	internal class MockFileProvider(string path) : IFileProvider
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
		public IChangeToken Watch(string filter) => throw new NotImplementedException();
	}

	internal class MockDirectoryContents(string name) : IDirectoryContents
	{
		public bool Exists => true;
		public IEnumerator<IFileInfo> GetEnumerator()
		{
			yield return new MockFileInfo(name, "exists.txt");
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal class MockFileInfo(string path, string name) : IFileInfo
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