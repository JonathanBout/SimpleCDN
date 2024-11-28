using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Cache
{
	public class CachedFile
	{
		private long _size;

		public CachedFile() { }

		public CompressionAlgorithm Compression { get; set; } = CompressionAlgorithm.None;
		public required long Size
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
	}

	public readonly struct CompressionAlgorithm : IEquatable<CompressionAlgorithm>
	{
		public static readonly CompressionAlgorithm GZip = new("gzip");
		public static readonly CompressionAlgorithm None = new("");
		public readonly string Name { get; } = "";
		private CompressionAlgorithm(string name)
		{
			Name = name;
		}

		public override string ToString() => Name;
		public bool Equals(CompressionAlgorithm other)
		{
			return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			if (obj is CompressionAlgorithm algorithm)
			{
				return Equals(algorithm);
			}
			return false;
		}

		public override int GetHashCode() => Name.GetHashCode(StringComparison.OrdinalIgnoreCase);

		public static implicit operator string(CompressionAlgorithm algorithm) => algorithm.Name;

		public static bool operator ==(CompressionAlgorithm left, CompressionAlgorithm right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(CompressionAlgorithm left, CompressionAlgorithm right)
		{
			return !(left == right);
		}
	}
}
