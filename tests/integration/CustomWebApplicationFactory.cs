using Microsoft.AspNetCore.Mvc.Testing;
using SimpleCDN.Configuration;

namespace SimpleCDN.Tests.Integration
{
	internal class CustomWebApplicationFactory : WebApplicationFactory<Standalone.Program>
	{
		public const string GENERATED_INDEX_ID = "!GENERATED!INDEX!";

		public string DataRoot => _rootDirectory.FullName;
		private readonly DirectoryInfo _rootDirectory = Directory.CreateTempSubdirectory("SimpleCDN-IntegrationTests");

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.ConfigureServices(services =>
			{
				services.Configure<CDNConfiguration>(config =>
				{
					config.Footer = GENERATED_INDEX_ID;
					config.DataRoot = DataRoot;
				});
			});
		}

		protected override void Dispose(bool disposing)
		{
			Directory.Delete(DataRoot, true);
		}
	}

	/// <summary>
	/// Wraps the <see cref="CustomWebApplicationFactory"/> to be able to use the internal <see cref="Program"/>
	/// class in the tests that have to be public.
	/// </summary>
	public sealed class CustomWebApplicationFactoryWrapper : IDisposable, IAsyncDisposable
	{
		private readonly CustomWebApplicationFactory _factory = new();

		public string DataRoot => _factory.DataRoot;

		public HttpClient CreateClient()
		{
			return _factory.CreateClient();
		}

		public void Dispose() => _factory.Dispose();
		public ValueTask DisposeAsync() => _factory.DisposeAsync();
	}
}
