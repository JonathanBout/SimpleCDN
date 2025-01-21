using SimpleCDN.Helpers;
using System.Net.Mime;

namespace SimpleCDN.Tests.Unit
{
	public class MimeTypeHelpersTests
	{
		[Test]
		public void MimeTypeFromFileName_Unknown()
		{
			ReadOnlySpan<char> name = "file.unknown";
			MimeType result = MimeTypeHelpers.MimeTypeFromFileName(name);
			Assert.That(result, Is.EqualTo(MimeType.Unknown));
		}

		[Test]
		[TestCase("file.html", MimeType.HTML)]
		[TestCase("file.htm", MimeType.HTML)]
		[TestCase("file.txt", MimeType.Plain)]
		[TestCase("file.json", MimeType.JSON)]
		[TestCase("file.md", MimeType.Markdown)]
		[TestCase("file.png", MimeType.PNG)]
		[TestCase("file.jpeg", MimeType.JPEG)]
		[TestCase("file.jpg", MimeType.JPEG)]
		[TestCase("file.gif", MimeType.GIF)]
		[TestCase("file.woff", MimeType.Woff)]
		[TestCase("file.woff2", MimeType.Woff2)]
		[TestCase("file.ttf", MimeType.TTF)]
		[TestCase("file.otf", MimeType.OTF)]
		[TestCase("file.css", MimeType.CSS)]
		[TestCase("file.eot", MimeType.EOT)]
		[TestCase("file.svg", MimeType.SVG)]
		[TestCase("file.webp", MimeType.WebP)]
		[TestCase("file.ico", MimeType.ICO)]
		public void MimeTypeFromFileName_Known(string filename, MimeType expected)
		{
			ReadOnlySpan<char> name = filename;
			MimeType result = MimeTypeHelpers.MimeTypeFromFileName(name);
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		[TestCase(MimeType.HTML, MediaTypeNames.Text.Html)]
		[TestCase(MimeType.CSS, MediaTypeNames.Text.Css)]
		[TestCase(MimeType.Plain, MediaTypeNames.Text.Plain)]
		[TestCase(MimeType.JSON, MediaTypeNames.Application.Json)]
		[TestCase(MimeType.Markdown, MediaTypeNames.Text.Markdown)]
		[TestCase(MimeType.PNG, MediaTypeNames.Image.Png)]
		[TestCase(MimeType.JPEG, MediaTypeNames.Image.Jpeg)]
		[TestCase(MimeType.SVG, MediaTypeNames.Image.Svg)]
		[TestCase(MimeType.WebP, MediaTypeNames.Image.Webp)]
		[TestCase(MimeType.GIF, MediaTypeNames.Image.Gif)]
		[TestCase(MimeType.Woff, MediaTypeNames.Font.Woff)]
		[TestCase(MimeType.Woff2, MediaTypeNames.Font.Woff2)]
		[TestCase(MimeType.TTF, MediaTypeNames.Font.Ttf)]
		[TestCase(MimeType.OTF, MediaTypeNames.Font.Otf)]
		[TestCase(MimeType.EOT, "application/vnd.ms-fontobject")]
		[TestCase(MimeType.ICO, MediaTypeNames.Image.Icon)]
		[TestCase(MimeType.Unknown, MediaTypeNames.Application.Octet)]
		public void ToContentTypeString(MimeType mimeType, string expected)
		{
			string result = mimeType.ToContentTypeString();
			Assert.That(result, Is.EqualTo(expected));
		}
	}
}
