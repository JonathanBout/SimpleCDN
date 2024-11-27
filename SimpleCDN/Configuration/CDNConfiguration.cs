using System.Diagnostics;
using System.Reflection;

namespace SimpleCDN.Configuration
{
	public class CDNConfiguration
	{
		private static string? _version;
		public static string Version
		{
			get
			{
				if (_version is not null) return _version;

				var parentDir = new FileInfo(AppContext.BaseDirectory).Directory;

				var versionFile = new FileInfo(Path.Combine(parentDir?.FullName ?? "/", ".version"));

				if (!versionFile.Exists) return _version = "0.0.1-dev";

				return _version = File.ReadAllText(versionFile.FullName);
			}
		}

		/// <summary>
		/// The data root path
		/// </summary>
		public string? DataRoot { get; set; }
		/// <summary>
		/// The maximum size of the in-memory cache in kB
		/// </summary>
		public uint MaxMemoryCacheSize { get; set; } = 500;

		public string Footer { get; set; } = $"""<a href="https://github.com/jonathanbout/simplecdn">Powered by SimpleCDN v{Version}</a>""";
		public string PageTitle { get; set; } = "SimpleCDN";
	}
}
