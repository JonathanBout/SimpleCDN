using SimpleCDN.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace SimpleCDN.Services.Compression
{
	public class BrotliCompressor : CompressorBase
	{
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Brotli;

		public override int MinimumSize => 50;

		public override Stream Compress(Stream data)
		{
			return new BrotliStream(data, CompressionMode.Compress, true);
		}

		public override Stream Decompress(Stream data)
		{
			return new BrotliStream(data, CompressionMode.Decompress, true);
		}
	}
}
