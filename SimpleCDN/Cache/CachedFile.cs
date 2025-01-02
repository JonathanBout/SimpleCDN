using SimpleCDN.Helpers;
using System.Buffers.Binary;

namespace SimpleCDN.Cache
{
	public class CachedFile
	{
		private int _size;

		public CachedFile() { }

		public CompressionAlgorithm Compression { get; set; } = CompressionAlgorithm.None;
		public required int Size
		{
			get
			{
				if (Compression == CompressionAlgorithm.None || _size <= 0)
				{
					return Content.Length;
				}

				return _size;
			}
			set
			{
				_size = value;
			}
		}

		public required byte[] Content { get; set; }
		public required MimeType MimeType { get; set; }
		public virtual DateTimeOffset LastModified { get; set; }

		/// <summary>
		/// Gets the size of a serialized <see cref="CachedFile"/> with the given content length.
		/// </summary>
		/// <param name="contentLength"></param>
		/// <returns></returns>
		//                                                         content size  algorithm id   content         real size     mime type      last modified
		private static int GetSerializedSize(int contentLength) => sizeof(int) + sizeof(uint) + contentLength + sizeof(int) + sizeof(uint) + sizeof(long);
		/// <summary>
		/// Gets the serialized size of this <see cref="CachedFile"/>.
		/// </summary>
		private int SerializedSize => GetSerializedSize(Content.Length);

		/// <summary>
		/// Serializes this <see cref="CachedFile"/> to a new <see cref="byte"/>[].
		/// </summary>
		/// <returns></returns>
		internal byte[] GetBytes()
		{
			var bytes = new byte[SerializedSize];
			var span = bytes.AsSpan();
			GetBytes(span);
			return bytes;
		}

		/// <summary>
		/// Serializes this <see cref="CachedFile"/> into the given <see cref="Span{T}"/>.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		internal virtual int GetBytes(Span<byte> bytes)
		{
			// format:
			//	content size				(int, 4 bytes)
			//	compression algorithm id	(uint, 4 bytes)
			//	content						(byte array, content size bytes)
			//	actual size					(int, 4 bytes) equal to content size if no compression
			//	mime type id				(uint, 4 bytes)
			//	last modified				(long, 8 bytes)

			if (bytes.Length < SerializedSize)
			{
				throw new ArgumentException($"Span is too short. Expected: {SerializedSize}. Actual: {bytes.Length}", nameof(bytes));
			}

			var offset = 0;

			// write content size
			BinaryPrimitives.WriteInt32LittleEndian(bytes[offset..], Content.Length);

			offset += sizeof(int);

			// write compression algorithm id
			BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], Compression.Id);

			offset += sizeof(uint);

			// write content
			Content.CopyTo(bytes[offset..]);

			offset += Content.Length;

			// write uncompressed size
			BinaryPrimitives.WriteInt32LittleEndian(bytes[offset..], Size);

			offset += sizeof(int);

			// write mime type id
			BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], (uint)MimeType);

			offset += sizeof(uint);

			// write last modified
			BinaryPrimitives.WriteInt64LittleEndian(bytes[offset..], LastModified.UtcTicks);

			return offset + sizeof(long);
		}

		internal static CachedFile? FromBytes(ReadOnlySpan<byte> bytes)
		{
			if (bytes.Length < sizeof(int))
			{
				return null;
			}

			var offset = 0;

			var contentSize = BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);

			if (bytes.Length < GetSerializedSize(contentSize))
			{
				return null;
			}

			offset += sizeof(int);

			var compression = CompressionAlgorithm.FromId(BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]));

			offset += sizeof(uint);

			var content = bytes[offset..(offset + contentSize)].ToArray();

			offset += content.Length;

			var realSize = BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);

			offset += sizeof(int);

			var mimeType = (MimeType)BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);

			offset += sizeof(uint);

			var lastModified = new DateTimeOffset(BinaryPrimitives.ReadInt64LittleEndian(bytes[offset..]), TimeSpan.Zero);

			return new()
			{
				Compression = compression,
				Content = content,
				MimeType = mimeType,
				LastModified = lastModified,
				Size = realSize
			};
		}
	}
}
