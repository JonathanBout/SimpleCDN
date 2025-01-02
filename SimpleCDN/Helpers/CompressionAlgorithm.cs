using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Helpers
{
	public readonly struct CompressionAlgorithm : IEquatable<CompressionAlgorithm>
	{
		public static readonly CompressionAlgorithm None = new("", "", 0, 0, 0);
		public static readonly CompressionAlgorithm GZip = new("gzip", ".gz", 1, sizeGrade: 1, speedGrade: 1);
		public static readonly CompressionAlgorithm Brotli = new("br", ".br", 2, sizeGrade: 10, speedGrade: 6);
		public static readonly CompressionAlgorithm Deflate = new("deflate", ".zz", 3, sizeGrade: 9, speedGrade: 7);
		public readonly string HttpName { get; } = "";
		public readonly string FileExtension { get; } = "";
		public readonly uint Id { get; } = 0;
		public readonly uint SizeGrade { get; } = 0;
		public readonly uint SpeedGrade { get; } = 0;
		private CompressionAlgorithm(string httpName, string fileExtension, uint id, uint sizeGrade, uint speedGrade)
		{
			HttpName = httpName;
			FileExtension = fileExtension;
			Id = id;
			SizeGrade = sizeGrade;
			SpeedGrade = speedGrade;
		}

		public override string ToString() => HttpName;
		public bool Equals(CompressionAlgorithm other)
		{
			return HttpName.Equals(other.HttpName, StringComparison.OrdinalIgnoreCase);
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
				"deflate" => Deflate,
				_ => None
			};
		}

		public static CompressionAlgorithm MostPreferred(PerformancePreference preference, params IEnumerable<string> names)
		{
			return MostPreferred(preference, names.Select(FromName));
		}

		public static CompressionAlgorithm MostPreferred(PerformancePreference preference, params IEnumerable<CompressionAlgorithm> algorithms)
		{
			return algorithms.OrderByDescending(a => preference switch
			{
				PerformancePreference.Size => a.SizeGrade,
				PerformancePreference.Speed => a.SpeedGrade,
				_ => a.SizeGrade + a.SpeedGrade
			}).FirstOrDefault(None);
		}

		public override int GetHashCode() => HttpName.GetHashCode(StringComparison.OrdinalIgnoreCase);

		public static implicit operator string(CompressionAlgorithm algorithm) => algorithm.HttpName;

		public static bool operator ==(CompressionAlgorithm left, CompressionAlgorithm right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(CompressionAlgorithm left, CompressionAlgorithm right)
		{
			return !(left == right);
		}
	}

	public enum PerformancePreference
	{
		None,
		Size,
		Speed
	}
}
