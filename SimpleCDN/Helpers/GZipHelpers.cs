using System.IO.Compression;

namespace SimpleCDN.Helpers
{
	public class GZipHelpers
	{
		/// <summary>
		/// Compresses the data using GZip, in-place. If the compressed data is not smaller than the original data, the original data is left unchanged.
		/// </summary>
		/// <param name="data">The data to compress</param>
		/// <returns><see langword="false"/> if the compressed data is not smaller than the original data. Otherwise, <see langword="true"/></returns>
		public static bool TryCompress(ref Span<byte> data)
		{
			return false;
#pragma warning disable CS0162 // Unreachable code detected. Caching is disabled for now, but the code is left here for future reference.
			using var memoryStream = new MemoryStream();
			using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
			{
				gzipStream.Write(data);
			}

			if (memoryStream.Length >= data.Length)
				return false;

			memoryStream.Position = 0;
			var read = memoryStream.Read(data);

			data = data[..read];

			return true;
#pragma warning restore CS0162 // Unreachable code detected
		}

		public static byte[] Decompress(byte[] data)
		{
			using var memoryStream = new MemoryStream();
			using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
			memoryStream.Write(data);

			using var decompressedStream = new MemoryStream();

			gzipStream.CopyTo(decompressedStream);

			return decompressedStream.ToArray();
		}
	}
}
