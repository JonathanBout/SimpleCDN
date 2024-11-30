using Microsoft.VisualBasic;
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

		//                            content size  algorithm id   content          real size     mime type      last modified
		private int SerializedSize => sizeof(int) + sizeof(uint) + Content.Length + sizeof(int) + sizeof(uint) + sizeof(long);

		internal byte[] GetBytes()
		{
			var bytes = new byte[SerializedSize];
			var span = bytes.AsSpan();
			GetBytes(ref span);
			return bytes;
		}

		internal virtual int GetBytes(ref Span<byte> bytes)
		{
			// format:
			//	content size				(int, 4 bytes)
			//	compression algorithm id	(uint, 4 bytes)
			//	content						(byte array, content size bytes)
			//	actual size					(int, 4 bytes) equal to content size if no compression
			//	mime type id				(uint, 4 bytes)
			//	last modified				(long, 8 bytes)

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

		internal static CachedFile FromBytes(ReadOnlySpan<byte> bytes)
		{
			var offset = 0;

			var contentSize = BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);

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
