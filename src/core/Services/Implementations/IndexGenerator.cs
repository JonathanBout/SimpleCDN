using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Security;
using System.Text;
using System.Web;

namespace SimpleCDN.Services.Implementations
{
	internal class IndexGenerator(IOptionsMonitor<CDNConfiguration> options, ILogger<IndexGenerator> logger, ICDNContext context) : IIndexGenerator
	{
		private readonly IOptionsMonitor<CDNConfiguration> _options = options;
		private readonly ILogger<IndexGenerator> _logger = logger;
		private readonly ICDNContext _context = context;
		public byte[]? GenerateIndex(string absolutePath, string rootRelativePath)
		{
			if (!Directory.Exists(absolutePath))
				return null;

			var directory = new DirectoryInfo(absolutePath);

			var index = new StringBuilder();

			var robotsMeta = _options.CurrentValue.BlockRobots ? "<meta name=\"robots\" content=\"noindex, nofollow\">" : "";

			// if the path is a single slash, we want to remove it
			// to show "Index of /" instead of "Index of //"
			if (rootRelativePath is "/")
			{
				rootRelativePath = "";
			}

			index.Append(
				$$"""
				<!DOCTYPE html>
				<html lang="en">
				<head>
					<meta charset="utf-8">
					{{robotsMeta}}
					<meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0, minimum-scale=1.0">
					<meta name="description" content="Index of /{{HttpUtility.HtmlAttributeEncode(_options.CurrentValue.PageTitle)}}">
					<link rel="stylesheet" href="{{_context.GetSystemFilePath("styles.css")}}">
					<title>{{HttpUtility.HtmlEncode(_options.CurrentValue.PageTitle)}} &middot; Index of /{{rootRelativePath}}</title>
				</head>
				<body>
				<header>
					<h1>Index of /{{rootRelativePath.Replace("/", "<wbr>/")}}</h1>
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
				""");

			if (rootRelativePath is not "/" and not "" && directory.Parent is DirectoryInfo parent)
			{
				AppendRow(index, "..", "Parent Directory", "parent.svg", -1, parent.LastWriteTimeUtc);
			}

			int rowsAdded = 0;

			foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
			{
				try
				{
					var name = subDirectory.Name;

					if (name.StartsWith('.') && !_options.CurrentValue.ShowDotFiles)
						continue;

					AppendRow(index, name + "/", name, "folder.svg", -1, subDirectory.LastWriteTimeUtc);
					rowsAdded++;
				} catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
				{
					_logger.LogError(ex, "Access denied to publicly available directory {directory} while generating an index", subDirectory.FullName.ForLog());
				}
			}

			foreach (FileInfo file in directory.EnumerateFiles())
			{
				try
				{

					var name = file.Name;

					if (name.StartsWith('.') && !_options.CurrentValue.ShowDotFiles)
						continue;

					AppendRow(index, name, name, "file.svg", file.Length, file.LastWriteTimeUtc);
				} catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
				{
					_logger.LogError(ex, "Access denied to publicly available file {file} while generating an index", file.FullName.ForLog());
				}
			}

			if (rowsAdded == 0)
			{
				index.Append("<tr><td colspan=\"4\">No files or directories to show</td></tr>");
			}

			index.AppendFormat("</tbody></table></main><footer>{0}</footer></body></html>", _options.CurrentValue.Footer);

			return Encoding.UTF8.GetBytes(index.ToString());
		}

		private void AppendRow(StringBuilder index, string href, string name, string iconName, long size, DateTimeOffset lastModified)
		{
			var iconPath = _context.GetSystemFilePath(iconName);

			index.Append("<tr>");
			index.AppendFormat("""<td class="col-icon"><img src="{0}" /></td>""", HttpUtility.HtmlAttributeEncode(iconPath));
			index.AppendFormat("""<td class="col-name"><a href="./{0}">{1}</a></td>""", HttpUtility.HtmlAttributeEncode(href), name);
			index.AppendFormat("""<td class="col-size">{0}</td>""", size < 0 ? "-" : size.FormatByteCount());
			index.AppendFormat("""<td class="col-date">{0}</td>""", lastModified.ToString("dd/MM/yyyy HH:mm"));
			index.Append("</tr>");
		}
	}
}
