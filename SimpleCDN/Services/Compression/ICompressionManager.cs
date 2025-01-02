using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Compression
{
	public interface ICompressionManager
	{
		Stream Compress(CompressionAlgorithm algorithm, Stream data);
		byte[] Decompress(CompressionAlgorithm algorithm, ReadOnlySpan<byte> data);
		Stream Decompress(CompressionAlgorithm algorithm, Stream data);
		void Compress(CompressionAlgorithm algorithm, Span<byte> data, out int newLength);
	}
}
