using SimpleCDN.Services.Compression;
using SimpleCDN.Services.Compression.Implementations;
using System.Text;

namespace SimpleCDN.Tests.Unit;

[TestFixture()]
public class CompressorTests
{
	[TestCase(JSON_CONTENT, 0u)]
	[TestCase(HTML_CONTENT, 0u)]
	[TestCase(JSON_CONTENT, 1u)]
	[TestCase(HTML_CONTENT, 1u)]
	[TestCase(JSON_CONTENT, 2u)]
	[TestCase(HTML_CONTENT, 2u)]
	public void Compress_Decompress(string inputText, uint algorithmId)
	{
		ICompressor compressor = FromId(algorithmId);

		var bytes = Encoding.UTF8.GetBytes(inputText);

		compressor.Compress(bytes, out int newLength);

		Assert.That(newLength, Is.LessThanOrEqualTo(bytes.Length));

		var decompressed = compressor.Decompress(bytes.AsSpan(..newLength));

		Assert.That(Encoding.UTF8.GetString(decompressed), Is.EqualTo(inputText));
	}

	[TestCase(SMALL_CONTENT, 0u)]
	[TestCase(SMALL_CONTENT, 1u)]
	[TestCase(SMALL_CONTENT, 2u)]
	public void Compress_SmallData_DoesNotGrowResult(string inputText, uint algorithmId)
	{
		ICompressor compressor = FromId(algorithmId);

		var data = Encoding.UTF8.GetBytes(inputText);
		Span<byte> dataSpan = data.AsSpan();

		compressor.Compress(dataSpan, out int newLength);

		Assert.That(inputText[..newLength], Is.EqualTo(Encoding.UTF8.GetString(data)));
	}

	static ICompressor FromId(uint id) => id switch
	{
		0 => new GZipCompressor(),
		1 => new BrotliCompressor(),
		2 => new DeflateCompressor(),
		_ => throw new AssertionException("Unknown algorithm id")
	};

	const string SMALL_CONTENT = "{}";

	const string JSON_CONTENT =
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
