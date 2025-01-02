using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Helpers
{
	public readonly struct CompressionAlgorithm : IEquatable<CompressionAlgorithm>
	{
		public static readonly CompressionAlgorithm None = new("", 0, 0);
		public static readonly CompressionAlgorithm GZip = new("gzip", 1, 1);
		public static readonly CompressionAlgorithm Brotli = new("br", 2, 2);
		public readonly string Name { get; } = "";
		public readonly uint Id { get; } = 0;
		public readonly uint Preference { get; } = 0;
		private CompressionAlgorithm(string name, uint id, uint preference)
		{
			Name = name;
			Id = id;
			Preference = preference;
		}

		public override string ToString() => Name;
		public bool Equals(CompressionAlgorithm other)
		{
			return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			if (obj is CompressionAlgorithm algorithm)
				return Equals(algorithm);
			return false;
		}

		public static CompressionAlgorithm FromId(uint id)
		{
			return id switch
			{
				0 => None,
				1 => GZip,
				2 => Brotli,
				_ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown compression algorithm id")
			};
		}

		public static CompressionAlgorithm FromName(string name)
		{
			return name.ToLower() switch
			{
				"gzip" => GZip,
				"br" or "brotli" => Brotli,
				_ => None
			};
		}

		public static CompressionAlgorithm MostPreferred(params IEnumerable<string> names)
		{
			return names.Select(FromName)
						.OrderByDescending(a => a.Preference)
						.FirstOrDefault(None);
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
