using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Cache
{
	public readonly struct CompressionAlgorithm : IEquatable<CompressionAlgorithm>
	{
		public static readonly CompressionAlgorithm None = new("", 0);
		public static readonly CompressionAlgorithm GZip = new("gzip", 1);
		public readonly string Name { get; } = "";
		public readonly uint Id { get; } = 0;
		private CompressionAlgorithm(string name, uint id)
		{
			Name = name;
			Id = id;
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

		public static CompressionAlgorithm FromId(uint id)
		{
			return id switch
			{
				0 => None,
				1 => GZip,
				_ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown compression algorithm id")
			};
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
