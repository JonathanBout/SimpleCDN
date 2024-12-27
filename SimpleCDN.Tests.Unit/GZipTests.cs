using SimpleCDN.Helpers;
using System.Text;

namespace SimpleCDN.Tests.Unit;

[TestFixture(TestName = "GZip Compression Tests")]
public class GZipTests
{
	[TestCase(JSON_CONTENT, TestName = "JSON Compression and Decompression")]
	[TestCase(HTML_CONTENT, TestName = "HTML Compression and Decompression")]
	public void Compress_Decompress(string inputText)
	{
		var data = Encoding.UTF8.GetBytes(inputText);
		Span<byte> dataSpan = data.AsSpan();

		var compressed = GZipHelpers.TryCompress(ref dataSpan);

		Assert.That(compressed, Is.True);

		var decompressed = GZipHelpers.Decompress(data);

		Assert.That(decompressed, Is.EqualTo(Encoding.UTF8.GetBytes(inputText)));
	}

	[TestCase("<html></html>", TestName = "Tiny HTML does not compress")]
	[TestCase("{}", TestName = "Tiny JSON does not compress")]
	public void Compress_SmallData_Fails(string inputText)
	{
		var data = Encoding.UTF8.GetBytes(inputText);
		Span<byte> dataSpan = data.AsSpan();
		var compressed = GZipHelpers.TryCompress(ref dataSpan);
		Assert.That(compressed, Is.False);
	}

	const string JSON_CONTENT =
	// language=JSON
	"""
		{
			"nested": "value",
			"nested2": {
				"nested3": "value",
				"nested4": [ "value1", "value2" ],
				"nested5": {
					"nested6": "value"
				}
			},
			"nested7": [
				{
					"nested8": "value"
				},
				{
					"nested9": "value"
				}
			]
		}
		""";

	const string HTML_CONTENT =
	// language=html
	"""
		<!DOCTYPE html>
		<html lang="en">
		<head>
			<meta charset="UTF-8">
			<meta name="viewport" content="width=device-width, initial-scale=1.0">
			<title>Document</title>
		</head>
		<body>
			<h1>Hello, World!</h1>
			<p>
				Lorem ipsum dolor sit amet, consectetur adipiscing elit.
				Nullam nec purus nec nunc tincidunt ultricies.
				Nullam nec pur us nec nunc tincidunt ultricies.
			</p>
		</body>
		</html>
		""";
}
