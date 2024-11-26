using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using System.Text;

namespace SimpleCDN.Helpers
{
	public class IndexGenerator(IOptionsMonitor<CDNConfiguration> options)
	{
		private readonly IOptionsMonitor<CDNConfiguration> _options = options;

		public byte[]? GenerateIndex(string absolutePath, string rootRelativePath)
		{
			if (!Directory.Exists(absolutePath))
			{
				return null;
			}

			var directory = new DirectoryInfo(absolutePath);

			var index = new StringBuilder();

			index.AppendFormat(
				"""
				<html>
				<head>
					<meta name="robots" content="noindex,nofollow">
					<link rel="stylesheet" href="/_cdn/styles.css">
					<meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0, minimum-scale=1.0">
				</head>
				<body>
				<header>
					<h1>Index of {0}</h1>
				</header>
				<main>
					<table>
						<thead><tr>
							<th>Name</th>
							<th>Size</th>
							<th>Last Modified (UTC)</th>
						</tr></thead>
						<tbody>
				""", rootRelativePath.Replace("/", "<wbr>/"));

			if (rootRelativePath is not "/" and not "" && directory.Parent is DirectoryInfo parent)
			{
				var lastSlashIndex = rootRelativePath.LastIndexOf('/');

				string parentRootRelativePath;

				if (lastSlashIndex is < 1)
				{
					parentRootRelativePath = "/";
				} else
				{
					parentRootRelativePath = rootRelativePath[..lastSlashIndex];
				}

				AppendRow(index, parentRootRelativePath, "Parent Directory", -1, parent.LastWriteTimeUtc);
			}

			foreach (var subDirectory in directory.EnumerateDirectories())
			{
				var name = subDirectory.Name;

				AppendRow(index, Path.Combine(rootRelativePath, name), name, -1, subDirectory.LastWriteTimeUtc);
			}

			foreach (var file in directory.EnumerateFiles())
			{
				var name = file.Name;

				AppendRow(index, Path.Combine(rootRelativePath, name), name, file.Length, file.LastWriteTimeUtc);
			}

			index.AppendFormat("</tbody></table></main><footer>{0}</footer></body></html>", _options.CurrentValue.Footer);

			var bytes = Encoding.UTF8.GetBytes(index.ToString());

			return bytes;
		}

		private static void AppendRow(StringBuilder index, string href, string name, long size, DateTimeOffset lastModified)
		{
			index.AppendFormat("""<tr><td><a href="{0}">{1}</a></td>""", href, name);
			index.AppendFormat("""<td>{0}</td>""", size < 0 ? "-" : size.FormatByteCount());
			index.AppendFormat("""<td>{0}</td></tr>""", lastModified.ToString("dd/MM/yyyy HH:mm"));
		}
	}
}
