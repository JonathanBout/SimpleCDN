using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;

namespace SimpleCDN.Helpers
{
	public static partial class Extensions
	{
		static readonly string[] sizeNames = ["", "k", "M", "G", "T"];

		public static string FormatByteCount(this long number)
		{
			var isNegative = false;
			if (number < 0)
			{
				isNegative = true;
				number = -number;
			}

			var sizeNameIndex = 0;

			double result = number;

			for (; sizeNameIndex < sizeNames.Length - 1; sizeNameIndex++)
			{
				var div = result / 1000;
				if (div < 1)
					break;

				result = div;
			}

			if (isNegative)
				result = -result;

			return $"{result:0.##}{sizeNames[sizeNameIndex]}B";
		}

		public static (T left, IEnumerable<T> right) RemoveFirst<T>(this IEnumerable<T> source)
		{
			var enumerator = source.GetEnumerator();

			if (!enumerator.MoveNext())
				ArgumentOutOfRangeException.ThrowIfLessThan(0, 1, nameof(source));

			return (enumerator.Current, RestEnumerator(enumerator));

			static IEnumerable<T> RestEnumerator(IEnumerator<T> enumerator)
			{
				while (enumerator.MoveNext())
				{
					yield return enumerator.Current;
				}
			}
		}

		public static void Normalize(ref this Span<char> path)
		{
			var segments = MemoryExtensions.Split(path, '/');

			var segmentsToRemove = new List<Range>();

			Range lastSegment = Range.All;

			foreach (var segment in segments)
			{
				if (segment.GetOffsetAndLength(path.Length).Length == 0)
				{
					continue;
				}

				if (path[segment] is ['.', '.'])
				{
					if (!lastSegment.Equals(Range.All))
					{
						segmentsToRemove.Add(lastSegment);
					}

					segmentsToRemove.Add(segment);

				} else if (path[segment] is ['.'])
				{
					// if the segment is . it should be removed
					segmentsToRemove.Add(segment);
				}

				lastSegment = segment;
			}

			int offset = 0;

			foreach (var segmentToRemove in segmentsToRemove)
			{
				// transform path, so that 

				var (start, length) = segmentToRemove.GetOffsetAndLength(path.Length);

				// include the / before the segment
				// and subtract the offset to account for previously removed segments
				start -= 1 + offset;
				length += 1;

				if (start < 0)
				{
					length += start;
					start = 0;

					// this full segment is now outside the current path, so we
					// can skip removing it
					if (length <= 0)
					{
						continue;
					}
				}

				var target = path[start..];

				// move the rest of the path to the left, overwriting the segment
				path[(start + length)..].CopyTo(target);

				// shorten the path
				path = path[..^length];

				offset += length;
			}
		}

		public static string ForLog(this string input) => WhitespaceRegex().Replace(input, " ");

		[GeneratedRegex(@"\s+", RegexOptions.Multiline | RegexOptions.Compiled)]
		private static partial Regex WhitespaceRegex();
	}
}
