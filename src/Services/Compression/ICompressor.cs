using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Compression
{
	/// <summary>
	/// Represents a compressor that can compress and decompress data using a specific <see cref="CompressionAlgorithm"/>.
	/// </summary>
	public interface ICompressor
	{
		/// <summary>
		/// The compression algorithm used by this compressor.
		/// </summary>
		CompressionAlgorithm Algorithm { get; }

		/// <summary>
		/// The minimum size of data that can be optimized by this compressor.
		/// </summary>
		int MinimumSize { get; }

		/// <summary>
		/// Compresses the data, in-place. If the compressed data is not smaller than the original data, the original data is left unchanged.
		/// </summary>
		bool Compress(Span<byte> data, out int newLength);

		/// <summary>
		/// Decompresses the data.
		/// </summary>
		byte[] Decompress(ReadOnlySpan<byte> data);

		/// <summary>
		/// Compresses the data.
		/// </summary>
		/// <returns>
		/// A stream which compresses the data when read.
		/// </returns>
		Stream Compress(Stream data);

		/// <summary>
		/// Decompresses the data.
		/// </summary>
		/// <returns>
		/// A stream which decompresses the data when read.
		/// </returns>
		Stream Decompress(Stream data);
	}
}
