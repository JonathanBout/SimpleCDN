using SimpleCDN.Helpers;
using SimpleCDN.Services.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Tests.Mocks
{
	public class MockCompressor : ICompressor, ICompressionManager
	{
		public CompressionAlgorithm Algorithm => CompressionAlgorithm.None;

		public int MinimumSize => 0;

		public Stream Compress(Stream data) => data;
		public void Compress(Span<byte> data, out int newLength) => newLength = data.Length;
		public Stream Compress(CompressionAlgorithm algorithm, Stream data) => Compress(data);
		public void Compress(CompressionAlgorithm algorithm, Span<byte> data, out int newLength) => Compress(data, out newLength);
		public byte[] Decompress(ReadOnlySpan<byte> data) => data.ToArray();
		public Stream Decompress(Stream data) => data;
		public byte[] Decompress(CompressionAlgorithm algorithm, ReadOnlySpan<byte> data) => Decompress(data);
		public Stream Decompress(CompressionAlgorithm algorithm, Stream data) => Decompress(data);
	}
}
