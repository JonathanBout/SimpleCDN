using SimpleCDN.Helpers;
using System.IO.Compression;
using System.Net.NetworkInformation;

namespace SimpleCDN.Services.Compression
{
	public class GZipCompressor : CompressorBase
	{
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.GZip;

		public override int MinimumSize => 48;

		public override Stream Compress(Stream data)
		{
			return new GZipStream(data, CompressionMode.Compress, true);
		}

		public override Stream Decompress(Stream data)
		{
			return new GZipStream(data, CompressionMode.Decompress, true);
		}
	}
}
