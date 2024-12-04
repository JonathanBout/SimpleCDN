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

		/// <summary>
		/// Normalizes a path by removing all . and .. segments in-place. When ready,
		/// <paramref name="path"/> will be shortened to contain just the normalized path.
		/// </summary>
		/// <param name="path">The path to normalize in-place</param>
		public static void Normalize(ref this Span<char> path)
		{
			var segments = MemoryExtensions.Split(path, '/');

			var rangesToRemove = new List<Range>();

			var resultRanges = new Stack<Range>();

			var originalLength = path.Length;

			foreach (var segment in segments)
			{
				if (segment.GetOffsetAndLength(originalLength).Length == 0)
				{
					continue;
				}

				if (path[segment] is ['.', '.'])
				{
					if (resultRanges.TryPop(out Range lastSegment))
					{
						rangesToRemove.Add(lastSegment);
					}

					rangesToRemove.Add(segment);

				} else if (path[segment] is ['.'])
				{
					// if the segment is . it should be removed
					rangesToRemove.Add(segment);
				} else
				{
					resultRanges.Push(segment);
				}
			}

			int offset = 0;

			var segmentsToRemove = rangesToRemove.Select(s => s.GetOffsetAndLength(originalLength)).OrderBy(s => s.Offset);

			foreach (var segment in segmentsToRemove)
			{
				var (start, length) = segment;

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

		/// <summary>
		/// Sanitizes a string for use in log messages:<br/>
		/// - replaces all whitespace (including newlines, tabs, ...) with a single space
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string ForLog(this string input) => WhitespaceRegex().Replace(input, " ");

		[GeneratedRegex(@"\s+", RegexOptions.Multiline | RegexOptions.Compiled)]
		private static partial Regex WhitespaceRegex();
	}
}
