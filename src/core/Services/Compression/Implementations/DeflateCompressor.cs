using SimpleCDN.Helpers;
using System.IO.Compression;

namespace SimpleCDN.Services.Compression.Implementations
{
	internal class DeflateCompressor : CompressorBase
	{
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Deflate;
		public override int MinimumSize => 40;
		public override Stream Compress(Stream data) => new DeflateStream(data, CompressionLevel.Optimal, true);
		public override Stream Decompress(Stream data) => new DeflateStream(data, CompressionMode.Decompress, true);
	}
}
