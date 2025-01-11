using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Compression
{
	/// <summary>
	/// Represents a manager service that can compress and decompress data using all supported compression algorithms.
	/// </summary>
	public interface ICompressionManager
	{
		/// <summary>
		/// Compresses the given <paramref name="data"/> using the specified <paramref name="algorithm"/>.
		/// </summary>
		/// <returns>
		/// A stream which compresses the data when read.
		/// </returns>
		Stream Compress(CompressionAlgorithm algorithm, Stream data);

		/// <summary>
		/// Decompresses the given <paramref name="data"/> using the specified <paramref name="algorithm"/>.
		/// </summary>
		byte[] Decompress(CompressionAlgorithm algorithm, ReadOnlySpan<byte> data);

		/// <summary>
		/// Decompresses the given <paramref name="data"/> using the specified <paramref name="algorithm"/>.
		/// </summary>
		/// <returns>
		/// A stream which decompresses the data when read.
		/// </returns>
		Stream Decompress(CompressionAlgorithm algorithm, Stream data);

		/// <summary>
		/// Attempts to compress the given <paramref name="data"/> using the specified <paramref name="algorithm"/>,
		/// in-place, and returns the new length of the data.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the data was compressed successfully; otherwise, <see langword="false"/>.
		/// </returns>
		bool Compress(CompressionAlgorithm algorithm, Span<byte> data, out int newLength);
	}
}
