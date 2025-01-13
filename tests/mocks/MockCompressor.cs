using SimpleCDN.Helpers;
using SimpleCDN.Services.Compression;

namespace SimpleCDN.Tests.Mocks
{
	public class MockCompressor : ICompressor, ICompressionManager
	{
		public CompressionAlgorithm Algorithm => CompressionAlgorithm.None;

		public int MinimumSize => 0;

		public Stream Compress(Stream data) => data;
		public bool Compress(Span<byte> data, out int newLength)
		{
			newLength = data.Length;
			return false;
		}
		public Stream Compress(CompressionAlgorithm algorithm, Stream data) => Compress(data);
		public bool Compress(CompressionAlgorithm algorithm, Span<byte> data, out int newLength) => Compress(data, out newLength);
		public byte[] Decompress(ReadOnlySpan<byte> data) => data.ToArray();
		public Stream Decompress(Stream data) => data;
		public byte[] Decompress(CompressionAlgorithm algorithm, ReadOnlySpan<byte> data) => Decompress(data);
		public Stream Decompress(CompressionAlgorithm algorithm, Stream data) => Decompress(data);
	}
}
