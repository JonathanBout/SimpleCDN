using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Compression
{
	public abstract class CompressorBase : ICompressor
	{
		public abstract CompressionAlgorithm Algorithm { get; }

		public abstract int MinimumSize { get; }

		public void Compress(Span<byte> data, out int newLength)
		{
			using var outputStream = new MemoryStream();
			using (Stream compressorStream = Compress(outputStream))
			{
				outputStream.Write(data);
			}
			outputStream.Position = 0;
			if (outputStream.Length >= data.Length)
			{
				newLength = data.Length;
				return;
			}
			newLength = outputStream.Read(data);
			data[newLength..].Clear();
		}

		public abstract Stream Compress(Stream data);
		public byte[] Decompress(ReadOnlySpan<byte> data)
		{
			using var outputStream = new MemoryStream();
			using (Stream decompressorStream = Decompress(outputStream))
			{
				outputStream.Write(data);
			}
			return outputStream.ToArray();
		}
		public abstract Stream Decompress(Stream data);
	}
}
