using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Internal;

namespace SimpleCDN.Tests.Mocks
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
}