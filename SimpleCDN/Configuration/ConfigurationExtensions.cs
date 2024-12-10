namespace SimpleCDN.Configuration
{
	public static class ConfigurationExtensions
	{
		const string InvalidConfigurationMessage = "See the log meessages for details.";

		public static IServiceCollection MapConfiguration(this IServiceCollection services)
		{
			services.AddOptionsWithValidateOnStart<CDNConfiguration>()
				.Configure<IConfiguration>((settings, configuration) =>
				{
					var cdnSection = configuration.GetSection("CDN");

					// First, try to bind to ENV variables
					settings.DataRoot = configuration["CDN_DATA_ROOT"] ?? settings.DataRoot;

					// Then, try to bind to the configuration file (possibly overwriting ENV variables)
					cdnSection.Bind(settings);

					// Finally, try to bind to the command line arguments (possibliy overwriting ENV variables and configuration file)
					settings.DataRoot = configuration["data-root"] ?? settings.DataRoot;
				})
				.Validate((CDNConfiguration config, ILogger<CDNConfiguration> logger) => config.Validate(logger), InvalidConfigurationMessage);

			return services;
		}
	}
}
