using System.IO.Compression;

namespace SimpleCDN.Helpers
{
	public class GZipHelpers
	{
		/// <summary>
		/// Compresses the data using GZip.
		/// </summary>
		/// <param name="data">The data to compress</param>
		/// <returns><see langword="false"/> if the compressed data is not smaller than the original data. Otherwise, <see langword="true"/></returns>
		public static bool TryCompress(ref Span<byte> data)
		{
			using var memoryStream = new MemoryStream();
			using var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress);
			gzipStream.Write(data);
			gzipStream.Flush();

			if (memoryStream.Length >= data.Length)
				return false;

			var read = memoryStream.Read(data);

			data = data[..read];

			return true;
		}
	}
}
