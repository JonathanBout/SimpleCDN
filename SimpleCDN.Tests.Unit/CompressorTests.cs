using SimpleCDN.Helpers;
using SimpleCDN.Services;
using SimpleCDN.Services.Compression;
using System.Text;

namespace SimpleCDN.Tests.Unit;

[TestFixture(TestName = "GZip Compression Tests")]
public class CompressorTests
{
	[TestCase(JSON_CONTENT)]
	[TestCase(HTML_CONTENT)]
	public void Compress_Decompress(string inputText)
	{
		var gzipData = Encoding.UTF8.GetBytes(inputText);
		var brotliData = Encoding.UTF8.GetBytes(inputText);

		Span<byte> gzipSpan = gzipData.AsSpan();
		Span<byte> brotliSpan = brotliData.AsSpan();

		var compressor = new CompressionManager([new GZipCompressor(), new BrotliCompressor()]);

		compressor.Compress(CompressionAlgorithm.GZip, gzipSpan, out int newLengthGzip);
		compressor.Compress(CompressionAlgorithm.Brotli, brotliSpan, out int newLengthBrotli);

		var decompressedGzip = compressor.Decompress(CompressionAlgorithm.GZip, gzipSpan[..newLengthGzip]);
		var decompressedBrotli = compressor.Decompress(CompressionAlgorithm.Brotli, brotliSpan[..newLengthBrotli]);

		Assert.Multiple(() =>
		{
			Assert.That(decompressedGzip, Is.EqualTo(Encoding.UTF8.GetBytes(inputText)));
			Assert.That(decompressedBrotli, Is.EqualTo(Encoding.UTF8.GetBytes(inputText)));
		});
	}

	[TestCase("<i></i>")]
	[TestCase("{}")]
	public void Compress_SmallData_Fails(string inputText)
	{
		var data = Encoding.UTF8.GetBytes(inputText);
		Span<byte> dataSpan = data.AsSpan();
		var compressor = new CompressionManager([new GZipCompressor()]);

		compressor.Compress(CompressionAlgorithm.GZip, dataSpan, out int newLength);
		Assert.That(inputText[..newLength], Is.EqualTo(Encoding.UTF8.GetString(data)));
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
