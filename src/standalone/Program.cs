using SimpleCDN.Configuration;
using SimpleCDN.Endpoints;

namespace SimpleCDN.Standalone
{
#pragma warning disable RCS1102 // This class can't be static because the integration tests want it as a type argument
	public class Program
	{
		private static void Main(string[] args)
		{
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

			WebApplication app = builder.Build();

			app.MapSimpleCDN();

			// health check endpoint
			app.MapGet("/" + GlobalConstants.SystemFilesRelativePath + "/server/health", () => "healthy");

			app.MapGet("/favicon.ico", () => Results.Redirect("/" + GlobalConstants.SystemFilesRelativePath + "/logo.ico", true));

			app.Run();
		}
	}
}
#pragma warning restore RCS1102 // Make class static
