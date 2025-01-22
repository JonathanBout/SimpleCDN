using Microsoft.Extensions.Configuration;
using SimpleCDN.Configuration;
using SimpleCDN.Endpoints;
using SimpleCDN.Services.Caching;
using SimpleCDN.Services.Caching.Implementations;
using TomLonghurst.ReadableTimeSpan;

namespace SimpleCDN.Standalone
{
#pragma warning disable RCS1102 // This class can't be static because the integration tests want it as a type argument
	public class Program
	{
		private static void Main(string[] args)
		{
			ReadableTimeSpan.EnableConfigurationBinding();

			WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

			// reconfigure the configuration to make sure we're using the right sources in the right order
			builder.Configuration.Sources.Clear();
			builder.Configuration
				.AddEnvironmentVariables()
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddCommandLine(args);

			builder.Services.AddSimpleCDN()
				.MapConfiguration(builder.Configuration);

			builder
				.Build()
				.MapSimpleCDN()
				.MapAdditionalEndpoints()
				.Run();
		}
	}
}
#pragma warning restore RCS1102 // Make class static
