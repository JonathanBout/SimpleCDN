using Microsoft.Extensions.Options;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System.Security;
using System.Text;
using System.Web;

namespace SimpleCDN.Services.Implementations
{
	internal class IndexGenerator(IOptionsMonitor<CDNConfiguration> options, ILogger<IndexGenerator> logger) : IIndexGenerator
	{
		private readonly IOptionsMonitor<CDNConfiguration> _options = options;
		private readonly ILogger<IndexGenerator> _logger = logger;

		public byte[]? GenerateIndex(string absolutePath, string rootRelativePath)
		{
			if (!Directory.Exists(absolutePath))
				return null;

			var directory = new DirectoryInfo(absolutePath);

			var index = new StringBuilder();

			var robotsMeta = _options.CurrentValue.BlockRobots ? "<meta name=\"robots\" content=\"noindex, nofollow\">" : "";

			index.Append(
				$$"""
				<!DOCTYPE html>
				<html lang="en">
				<head>
					<meta charset="utf-8">
					{{robotsMeta}}
					<meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0, minimum-scale=1.0">
					<meta name="description" content="Index of /{{_options.CurrentValue.PageTitle.Replace("\"", "")}}">
					<svg style="display: none;" version="2.0">
					<defs>
						{{FOLDER_ICON}}
						{{FILE_ICON}}
						{{PARENT_ICON}}
						{{SIMPLECDN_LOGO}}
					</defs>
					</svg>
					<style>
						{{INDEX_CSS}}
					</style>
					<title>{{_options.CurrentValue.PageTitle}} &middot; Index of /{{rootRelativePath}}</title>
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
				AppendRow(index, "..", "Parent Directory", Icons.Parent, -1, parent.LastWriteTimeUtc);
			}

			try
			{
				foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
				{
					var name = subDirectory.Name;

					if (name.StartsWith('.') && !_options.CurrentValue.ShowDotFiles)
						continue;

					AppendRow(index, name + "/", name, Icons.Folder, -1, subDirectory.LastWriteTimeUtc);
				}

				foreach (FileInfo file in directory.EnumerateFiles())
				{
					var name = file.Name;

					if (name.StartsWith('.') && !_options.CurrentValue.ShowDotFiles)
						continue;

					AppendRow(index, name, name, Icons.File, file.Length, file.LastWriteTimeUtc);
				}
			} catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
			{
				_logger.LogError(ex, "Access denied to publicly available directory {directory} while generating an index", directory.FullName);

				return null;
			}

			index.AppendFormat("</tbody></table></main><footer>{0}</footer></body></html>", _options.CurrentValue.Footer);

			return Encoding.UTF8.GetBytes(index.ToString());
		}

		private static void AppendRow(StringBuilder index, string href, string name, Icons icon, long size, DateTimeOffset lastModified)
		{
			var iconTag = GetIcon(icon);
			index.Append("<tr>");
			index.AppendFormat("""<td class="col-icon">{0}</td>""", iconTag);
			index.AppendFormat("""<td class="col-name"><a href="./{0}">{1}</a></td>""", HttpUtility.HtmlAttributeEncode(href), name);
			index.AppendFormat("""<td class="col-size">{0}</td>""", size < 0 ? "-" : size.FormatByteCount());
			index.AppendFormat("""<td class="col-date">{0}</td>""", lastModified.ToString("dd/MM/yyyy HH:mm"));
			index.Append("</tr>");
		}

		enum Icons
		{
			Folder,
			File,
			Parent,
			SimpleCDN
		}

		private static string GetIcon(Icons icon)
		{
			string iconId = icon switch
			{
				Icons.Folder => "folder-icon",
				Icons.File => "file-icon",
				Icons.Parent => "parent-icon",
				Icons.SimpleCDN => "simplecdn-logo",
				_ => throw new ArgumentOutOfRangeException(nameof(icon), icon, null)
			};

			return $"""<svg width="24" height="24" version="2.0"><use href="#{iconId}"></use></svg>""";
		}

		const string FOLDER_ICON = """
			<symbol id="folder-icon" viewBox="0 0 24 24" width="24" height="24" fill="#333">
				<path d="M10 4H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-8l-2-2z"/>
			</symbol>
			""";

		const string FILE_ICON = """
			<symbol id="file-icon" viewBox="0 0 24 24" width="24" height="24" fill="#333">
				<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6z"/>
				<path d="M14 2v6h6"/>
			</symbol>
			""";

		const string PARENT_ICON = """
			<symbol id="parent-icon" viewBox="0 0 24 24" width="24" height="24" fill="#333">
				<path d="M10 4H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-8l-2-2z"/>
				<path d="M12 10l-4 4h3v4h2v-4h3l-4-4z" fill="white"/>
			</symbol>
			""";

		// If you update this icon, be sure to copy it to wwwroot/logo.svg and regenerate logo.ico
		const string SIMPLECDN_LOGO = """
			<symbol id="simplecdn-logo" viewBox="0 0 24 24" width="24" height="24" fill="transparent">
				<path d="M10 4H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-8l-2-2z" fill="#333" stroke="#888" />
				<path d="M7 12h10a0.5 0.5 0 0 1 0 1H7a0.5 0.5 0 0 1 0-1zm2 3h6a0.5 0.5 0 0 1 0 1H9a0.5 0.5 0 0 1 0-1z" fill="#fff"/>
				<path d="M5 9h14a0.5 0.5 0 0 1 0 1H5a0.5 0.5 0 0 1 0-1z" fill="#fff"/>
			</symbol>
			""";

		const string INDEX_CSS = """
			@font-face {
				font-family: Code;
				font-display: swap;
				src: local("Cascadia Code"),url(/CascadiaCode/woff2/CascadiaCode.woff2) format("woff2"),url(/CascadiaCode/ttf/CascadiaCode.ttf) format("truetype"),url(/CascadiaCode/CascadiaCode.ttf) format("truetype"),url(/CascadiaCode/otf/static/CascadiaCodeNF-Regular.otf) format("opentype"),local("Cascadia Mono");
			}

			:root {
				font-family: Code, monospace;

				--text-color: black;
				--background-color: white;
				--link-color: #0000EE;
				--hover-color: #ccc;
			}

			@media(prefers-color-scheme: dark) {
				:root {
					--text-color: #eee;
					--background-color: #111;
					--link-color: #9999ff;
					--hover-color: #222;
				}
			}

			* {
				box-sizing: border-box;
				text-align: center;
			}

			body {
				display: flex;
				flex-direction: column;
				justify-content: space-between;
				min-height: 100vh;
				margin: 0;
				padding: 0;
				padding-block: 5px;
				max-width: 100vw;
				overflow-x: hidden;
				background-color: var(--background-color);
				color: var(--text-color);
			}

			main {
				overflow: auto;
				flex-grow: 1;
			}

			a {
				color: var(--link-color);

				text-underline-offset: .25em;
			}

			table {
				margin: auto;
				overflow: auto;
				border-collapse: collapse;
			}

			h1 {
				text-align: center;
				overflow: auto;
				padding-inline: 5px;

			}

			th, td {
				padding: .2em 1em;
			}

			.col-size, .col-date {
				text-align: center;
			}

			.col-name {
				text-align: start;
			}

			thead th {
				font-size: 1.2rem;
				border-bottom: 1px solid var(--text-color);
			}

			tr {
				position: relative;
				transition: background-color .5s;
			}

			tr:has(a):hover {
				transition: background-color 200ms;
				background-color: var(--hover-color);
			}

			.col-name a::after {
				content: '';
				position: absolute;
				inset: 0;
			}

			footer {
				text-align: center;
			}

			img {
				transform: translateY(0);
			}

			td:has(img) {
				display: inline-flex;
				align-items: center;
			}
			""";
	}
}
