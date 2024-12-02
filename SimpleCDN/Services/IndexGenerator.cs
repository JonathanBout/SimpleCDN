using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Security;
using System.Text;

namespace SimpleCDN.Services
{
	public class IndexGenerator(IOptionsMonitor<CDNConfiguration> options, ILogger<IndexGenerator> logger) : IIndexGenerator
	{
		private readonly IOptionsMonitor<CDNConfiguration> _options = options;
		private readonly ILogger<IndexGenerator> _logger = logger;

		public byte[]? GenerateIndex(string absolutePath, string rootRelativePath)
		{
			if (!Directory.Exists(absolutePath))
				return null;

			var directory = new DirectoryInfo(absolutePath);

			var index = new StringBuilder();

			index.AppendFormat(
				"""
				<html>
				<head>
					<meta name="robots" content="noindex,nofollow">
					<link rel="stylesheet" href="/_cdn/styles.css">
					<meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0, minimum-scale=1.0">
					<link rel="icon" href="/_cdn/logo.svg" type="image/svg+xml">
					<title>{1} &middot; Index of {2}</title>
				</head>
				<body>
				<header>
					<h1>Index of {0}</h1>
				</header>
				<main>
					<table>
						<thead><tr>
							<th class="col-icon"></th>
							<th class="col-name">Name</th>
							<th class="col-size">Size</th>
							<th class="col-date">Last Change (UTC)</th>
						</tr></thead>
						<tbody>
				""", rootRelativePath.Replace("/", "<wbr>/"), _options.CurrentValue.PageTitle, rootRelativePath);

			if (rootRelativePath is not "/" and not "" && directory.Parent is DirectoryInfo parent)
			{
				var lastSlashIndex = rootRelativePath.LastIndexOf('/');

				string parentRootRelativePath;

				if (lastSlashIndex is < 1)
					parentRootRelativePath = "/";
				else
				{
					parentRootRelativePath = rootRelativePath[..lastSlashIndex];
				}

				AppendRow(index, parentRootRelativePath, "Parent Directory", "parent", -1, parent.LastWriteTimeUtc);
			}

			try
			{

				foreach (var subDirectory in directory.EnumerateDirectories())
				{
					var name = subDirectory.Name;

					AppendRow(index, Path.Combine(rootRelativePath, name), name, "folder", -1, subDirectory.LastWriteTimeUtc);
				}

				foreach (var file in directory.EnumerateFiles())
				{
					var name = file.Name;

					AppendRow(index, Path.Combine(rootRelativePath, name), name, "file", file.Length, file.LastWriteTimeUtc);
				}
			} catch (SecurityException ex)
			{
				_logger.LogError(ex, "Access denied to publicly available directory {directory} while generating an index", directory.FullName);

				return null;
			}

			index.AppendFormat("</tbody></table></main><footer>{0}</footer></body></html>", _options.CurrentValue.Footer);

			var bytes = Encoding.UTF8.GetBytes(index.ToString());

			return bytes;
		}

		private static void AppendRow(StringBuilder index, string href, string name, string icon, long size, DateTimeOffset lastModified)
		{
			index.Append("<tr>");
			index.AppendFormat("""<td class="col-icon"><img src="/_cdn/{0}.svg" alt="{0}"></img></td>""", icon);
			index.AppendFormat("""<td class="col-name"><a href="{0}">{1}</a></td>""", href, name);
			index.AppendFormat("""<td class="col-size">{0}</td>""", size < 0 ? "-" : size.FormatByteCount());
			index.AppendFormat("""<td class="col-date">{0}</td>""", lastModified.ToString("dd/MM/yyyy HH:mm"));
			index.Append("</tr>");
		}
	}
}
