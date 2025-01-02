using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Compression
{
	public class CompressionManager(IEnumerable<ICompressor> availableCompressors) : ICompressionManager
	{
		private readonly Dictionary<CompressionAlgorithm, ICompressor> _compressors = availableCompressors.ToDictionary(c => c.Algorithm);

		public Stream Compress(CompressionAlgorithm algorithm, Stream data)
		{
			if (!_compressors.TryGetValue(algorithm, out ICompressor? compressor))
				return data;

			return compressor.Compress(data);
		}

		public Stream Decompress(CompressionAlgorithm algorithm, Stream data)
		{
			if (!_compressors.TryGetValue(algorithm, out ICompressor? compressor))
				return data;
			return compressor.Decompress(data);
		}

		public byte[] Decompress(CompressionAlgorithm algorithm, ReadOnlySpan<byte> data)
		{
			if (!_compressors.TryGetValue(algorithm, out ICompressor? compressor))
				return data.ToArray();
			return compressor.Decompress(data);
		}

		public bool Compress(CompressionAlgorithm algorithm, Span<byte> data, out int newLength)
		{
			if (!_compressors.TryGetValue(algorithm, out ICompressor? compressor) || data.Length < compressor.MinimumSize)
			{
				// no compressor for the selected algorithm,
				// or the data is too small to be compressed without loss
				newLength = data.Length;
				return false;
			} else
			{
				return compressor.Compress(data, out newLength);
			}
		}
	}
}
