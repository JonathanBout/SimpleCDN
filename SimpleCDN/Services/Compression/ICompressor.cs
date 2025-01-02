using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Compression
{
	public interface ICompressor
	{
		CompressionAlgorithm Algorithm { get; }
		int MinimumSize { get; }

		/// <summary>
		/// Compresses the data, in-place. If the compressed data is not smaller than the original data, the original data is left unchanged.
		/// </summary>
		bool Compress(Span<byte> data, out int newLength);

		/// <summary>
		/// Decompresses the data.
		/// </summary>
		byte[] Decompress(ReadOnlySpan<byte> data);

		Stream Compress(Stream data);
		Stream Decompress(Stream data);
	}
}
