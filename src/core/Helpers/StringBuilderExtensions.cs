using System.Diagnostics;
using System.Text;

namespace SimpleCDN.Helpers
{
	internal static class StringBuilderExtensions
	{
		/// <summary>
		/// Converts the StringBuilder to a byte array using the specified encoding.
		/// This is more efficient than calling `Encoding.GetBytes(sb.ToString())`,
		/// because it avoids creating a copy of the string.
		/// </summary>
		/// <param name="sb">The StringBuilder to convert.</param>
		/// <param name="encoding">The encoding to use. If not specified, <see cref="Encoding.UTF8"/> will be used.</param>
		public static byte[] ToByteArray(this StringBuilder sb, Encoding? encoding = null)
		{
			// to make sure we only have to allocate the byte array once, we need to calculate the total length first
			// a downside of this approach is that we need to iterate over the chunks twice, but as the benchmark shows,
			// this method is still faster than using `Encoding.GetBytes(sb.ToString())` for large strings

			encoding ??= Encoding.UTF8;


			// if the encoding is single-byte, we don't need to iterate over the chunks twice,
			// as the length of the byte array will be the same as the length of the string.
			int totalLength = sb.Length;

			if (!encoding.IsSingleByte)
			{
				StringBuilder.ChunkEnumerator firstChunksEnumerator = sb.GetChunks();
				totalLength = 0;
				while (firstChunksEnumerator.MoveNext())
				{
					totalLength += encoding.GetByteCount(firstChunksEnumerator.Current.Span);
				}
			}

			// now that we know the total length, we can allocate the byte array
			var bytes = new byte[totalLength];
			// use a span for faster and easier slicing
			Span<byte> bytesSpan = bytes.AsSpan();

			// allocate the second enumerator
			StringBuilder.ChunkEnumerator chunksEnumerator = sb.GetChunks();
			int bytesOffset = 0;

			// iterate over the chunks again, and copy them to the allocated byte array
			while (chunksEnumerator.MoveNext())
			{
				bytesOffset += encoding.GetBytes(chunksEnumerator.Current.Span, bytesSpan[bytesOffset..]);
			}

			Debug.Assert(bytesOffset == totalLength);

			return bytes;
		}
	}
}
