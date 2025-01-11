using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Helpers
{
	/// <summary>
	/// Represents a compression algorithm that can be used to compress files.
	/// </summary>
	public readonly struct CompressionAlgorithm : IEquatable<CompressionAlgorithm>
	{
		/// <summary>
		/// Represents no compression.
		/// </summary>
		public static readonly CompressionAlgorithm None = new("", "", 0, 0, 0);

		/// <summary>
		/// Represents the GZip compression algorithm.
		/// </summary>
		public static readonly CompressionAlgorithm GZip = new("gzip", ".gz", 1, sizeGrade: 1, speedGrade: 1);

		/// <summary>
		/// Represents the Brotli compression algorithm.
		/// </summary>
		public static readonly CompressionAlgorithm Brotli = new("br", ".br", 2, sizeGrade: 10, speedGrade: 6);

		/// <summary>
		/// Represents the Deflate compression algorithm.
		/// </summary>
		public static readonly CompressionAlgorithm Deflate = new("deflate", ".zz", 3, sizeGrade: 8, speedGrade: 7);

		/// <summary>
		/// The name of the compression algorithm as used in HTTP headers.
		/// </summary>
		/// <example>
		/// <c>br</c> for Brotli.
		/// </example>
		public readonly string HttpName { get; } = "";

		/// <summary>
		/// The file extension used for files compressed with this algorithm.
		/// </summary>
		public readonly string FileExtension { get; } = "";

		/// <summary>
		/// The internal unique identifier of the compression algorithm.
		/// </summary>
		internal readonly uint Id { get; } = 0;

		/// <summary>
		/// The grade of the compression algorithm in terms of size reduction.
		/// This is relative to the other algorithms and may change when new algorithms are added.
		/// </summary>
		public readonly uint SizeGrade { get; } = 0;

		/// <summary>
		/// The grade of the compression algorithm in terms of speed.
		/// This is relative to the other algorithms and may change when new algorithms are added.
		/// </summary>
		public readonly uint SpeedGrade { get; } = 0;

		private CompressionAlgorithm(string httpName, string fileExtension, uint id, uint sizeGrade, uint speedGrade)
		{
			HttpName = httpName;
			FileExtension = fileExtension;
			Id = id;
			SizeGrade = sizeGrade;
			SpeedGrade = speedGrade;
		}

		/// <summary>
		/// Returns the <see cref="HttpName"/> of the compression algorithm, or "None" if no name is set.
		/// </summary>
		public override string ToString() => string.IsNullOrWhiteSpace(HttpName) ? "None" : HttpName;

		/// <summary>
		/// compares two compression algorithms by their <see cref="HttpName"/>.
		/// </summary>
		public bool Equals(CompressionAlgorithm other)
		{
			return HttpName.Equals(other.HttpName, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// compares two objects by their <see cref="HttpName"/>.
		/// </summary>
		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			if (obj is CompressionAlgorithm algorithm)
				return Equals(algorithm);
			return false;
		}

		internal static CompressionAlgorithm FromId(uint id)
		{
			return id switch
			{
				0 => None,
				1 => GZip,
				2 => Brotli,
				3 => Deflate,
				_ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown compression algorithm id")
			};
		}

		/// <summary>
		/// Returns the compression algorithm with the given <see cref="HttpName"/>, or <see cref="None"/> if no algorithm matches.
		/// </summary>
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

		/// <summary>
		/// Returns the most preferred compression algorithm from the given list of <see cref="HttpName"/>s, based on the given <see cref="PerformancePreference"/>.
		/// </summary>
		public static CompressionAlgorithm MostPreferred(PerformancePreference preference, params IEnumerable<string> names)
		{
			return MostPreferred(preference, names.Select(FromName));
		}

		/// <summary>
		/// Returns the most preferred compression algorithm from the given list of algorithms, based on the given <see cref="PerformancePreference"/>.
		/// </summary>
		public static CompressionAlgorithm MostPreferred(PerformancePreference preference, params IEnumerable<CompressionAlgorithm> algorithms)
		{
			return algorithms.OrderByDescending(a => preference switch
			{
				PerformancePreference.Size => a.SizeGrade,
				PerformancePreference.Speed => a.SpeedGrade,
				_ => a.SizeGrade + a.SpeedGrade
			}).FirstOrDefault(None);
		}

		/// <inheritdoc/>
		public override int GetHashCode() => HttpName.GetHashCode(StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Converts a <see cref="CompressionAlgorithm"/> to a <see cref="string"/> by its <see cref="HttpName"/>.
		/// </summary>
		public static implicit operator string(CompressionAlgorithm algorithm) => algorithm.HttpName;

		/// <summary>
		/// Compares two <see cref="CompressionAlgorithm"/>s by their <see cref="HttpName"/>.
		/// </summary>
		public static bool operator ==(CompressionAlgorithm left, CompressionAlgorithm right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Compares two <see cref="CompressionAlgorithm"/>s by their <see cref="HttpName"/>.
		/// </summary>
		public static bool operator !=(CompressionAlgorithm left, CompressionAlgorithm right)
		{
			return !(left == right);
		}
	}

	/// <summary>
	/// Represents a preference for performance when selecting a compression algorithm.
	/// </summary>
	public enum PerformancePreference
	{
		/// <summary>
		/// No preference. A combination of size and speed will be used.
		/// </summary>
		None,
		/// <summary>
		/// Prefer smaller files.
		/// </summary>
		Size,
		/// <summary>
		/// Prefer faster compression.
		/// </summary>
		Speed
	}
}
