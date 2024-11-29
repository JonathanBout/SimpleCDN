using System.Net.Mime;

namespace SimpleCDN.Helpers
{
	internal static class MimeTypeHelpers
	{
		public static MimeType MimeTypeFromFileName(string name)
		{
			var extension = name[(name.LastIndexOf('.') + 1)..];

			return extension.ToLower() switch
			{
				"html" or "htm" => MimeType.HTML,
				"txt" => MimeType.Text,
				"png" => MimeType.PNG,
				"json" => MimeType.JSON,
				"jpeg" or "jpg" => MimeType.JPEG,
				"gif" => MimeType.GIF,
				"woff" => MimeType.Woff,
				"woff2" => MimeType.Woff2,
				"ttf" => MimeType.TTF,
				"otf" => MimeType.OTF,
				"css" => MimeType.CSS,
				"eot" => MimeType.EOT,
				"svg" => MimeType.SVG,
				"webp" => MimeType.WebP,
				_ => MimeType.Unknown
			};
		}

		public static string ToContentTypeString(this MimeType type)
		{
			return type switch
			{
				MimeType.HTML => MediaTypeNames.Text.Html,
				MimeType.CSS => MediaTypeNames.Text.Css,
				MimeType.Text => MediaTypeNames.Text.Plain,
				MimeType.JSON => MediaTypeNames.Application.Json,
				MimeType.PNG => MediaTypeNames.Image.Png,
				MimeType.JPEG => MediaTypeNames.Image.Jpeg,
				MimeType.SVG => MediaTypeNames.Image.Svg,
				MimeType.WebP => MediaTypeNames.Image.Webp,
				MimeType.GIF => MediaTypeNames.Image.Gif,
				MimeType.Woff => MediaTypeNames.Font.Woff,
				MimeType.Woff2 => MediaTypeNames.Font.Woff2,
				MimeType.TTF => MediaTypeNames.Font.Ttf,
				MimeType.OTF => MediaTypeNames.Font.Otf,
				MimeType.EOT => "application/vnd.ms-fontobject",
				_ => MediaTypeNames.Application.Octet,
			};
		}

		public static readonly (MimeType, byte[]?) Empty = (MimeType.Unknown, null);
	}
}
