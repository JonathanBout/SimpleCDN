using System.Net.Mime;

namespace SimpleCDN
{
	internal static class MimeTypeHelpers
	{
		public static MimeType MimeTypeFromFileName(string name)
		{
			var extension = name[(name.LastIndexOf('.') + 1)..];

			return extension.ToLower() switch
			{
				"html" or "htm" => MimeType.HTML,
				"txt" => MimeType.TEXT,
				"png" => MimeType.PNG,
				"json" => MimeType.JSON,
				"jpeg" or "jpg" => MimeType.JPEG,
				"gif" => MimeType.GIF,
				"woff" => MimeType.WOFF,
				"woff2" => MimeType.WOFF2,
				"ttf" => MimeType.TTF,
				"otf" => MimeType.OTF,
				"css" => MimeType.CSS,
				"eot" => MimeType.EOT,
				"svg" => MimeType.SVG,
				"webp" => MimeType.WEBP,
				_ => MimeType.UNKNOWN
			};
		}

		public static string ToContentTypeString(this MimeType type)
		{
			return type switch
			{
				MimeType.HTML => MediaTypeNames.Text.Html,
				MimeType.CSS => MediaTypeNames.Text.Css,
				MimeType.TEXT => MediaTypeNames.Text.Plain,
				MimeType.JSON => MediaTypeNames.Application.Json,
				MimeType.PNG => MediaTypeNames.Image.Png,
				MimeType.JPEG => MediaTypeNames.Image.Jpeg,
				MimeType.SVG => MediaTypeNames.Image.Svg,
				MimeType.WEBP => MediaTypeNames.Image.Webp,
				MimeType.GIF => MediaTypeNames.Image.Gif,
				MimeType.WOFF => MediaTypeNames.Font.Woff,
				MimeType.WOFF2 => MediaTypeNames.Font.Woff2,
				MimeType.TTF => MediaTypeNames.Font.Ttf,
				MimeType.OTF => MediaTypeNames.Font.Otf,
				MimeType.EOT => "application/vnd.ms-fontobject",
				_ => MediaTypeNames.Application.Octet,
			};
		}

		public static readonly (MimeType, byte[]?) Empty = (MimeType.UNKNOWN, null);
	}
}
