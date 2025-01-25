using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Compression.Implementations
{
	internal abstract class CompressorBase : ICompressor
	{
		public abstract CompressionAlgorithm Algorithm { get; }

		public abstract int MinimumSize { get; }

		public bool Compress(Span<byte> data, out int newLength)
		{
			if (data.Length < MinimumSize)
			{
				// the data is too small to be compressed
				newLength = data.Length;
				return false;
			}

			using var outputStream = new MemoryStream();
			using (Stream compressorStream = Compress(outputStream))
			{
				compressorStream.Write(data);
			}

			if (outputStream.Length >= data.Length)
			{
				// the compressed data is not smaller than the original data,
				// so we return with the unchanged input data
				newLength = data.Length;
				return false;
			}

			outputStream.Position = 0;
			newLength = outputStream.Read(data);
			data[newLength..].Clear();
			return true;
		}

		public byte[] Decompress(ReadOnlySpan<byte> data)
		{
			using var outputStream = new MemoryStream();
			using var inputStream = new MemoryStream();
			inputStream.Write(data);
			inputStream.Position = 0;
			using Stream decompressorStream = Decompress(inputStream);
			decompressorStream.CopyTo(outputStream);
			return outputStream.ToArray();
		}
		public abstract Stream Compress(Stream data);
		public abstract Stream Decompress(Stream data);
	}
}
