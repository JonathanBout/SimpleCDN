using Microsoft.AspNetCore.Mvc.Testing;
using SimpleCDN.Configuration;

namespace SimpleCDN.Tests.Integration
{
	public class CustomWebApplicationFactory : WebApplicationFactory<Program>
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
}
