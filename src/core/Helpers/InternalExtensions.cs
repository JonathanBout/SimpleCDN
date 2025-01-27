using System.Text;

namespace SimpleCDN.Helpers
{
	internal static class InternalExtensions
	{
		// only up to exabytes, because 1 yottabyte doesn't even fit in a ulong
		// (I also don't think we are going to need it anytime soon)
		static readonly string[] sizeNames = ["k", "M", "G", "T", "P", "E"];

		public static string FormatByteCount(this long number)
		{
			if (number is < 1000 and > -1000)
			{
				return number + "B";
			}

			int sizeNameIndex = 0;
			double result = double.Abs(number);

			while (result >= 1000 && sizeNameIndex < sizeNames.Length)
			{
				result /= 1000;
				sizeNameIndex++;
			}

			return $"{(number < 0 ? "-": "")}{result:0.##}{sizeNames[sizeNameIndex - 1]}B";
		}

		/// <summary>
		/// Normalizes a path by removing all . and .. segments in-place. Leading and trailing slashes
		/// are also trimmed from the result. When ready, <paramref name="path"/> will be shortened
		/// to contain just the normalized path.
		/// </summary>
		/// <param name="path">The path to normalize in-place.</param>
		public static void Normalize(ref this Span<char> path)
		{
			IEnumerable<Range> segments = path.SplitToPathSegments();

			var rangesToRemove = new List<Range>();

			var resultRanges = new Stack<Range>();

			var originalLength = path.Length;

			foreach (Range segment in segments)
			{
				if (path[segment] is "..")
				{
					if (resultRanges.TryPop(out Range lastSegment))
					{
						rangesToRemove.Add(lastSegment);
					}

					rangesToRemove.Add(segment);
				} else if (path[segment] is ".")
				{
					// if the segment is . it should be removed
					rangesToRemove.Add(segment);
				} else
				{
					resultRanges.Push(segment);
				}
			}

			int offset = 0;

			foreach ((int Offset, int Length) segment in rangesToRemove
															.Select(s => s.GetOffsetAndLength(originalLength))
															.OrderBy(s => s.Offset))
			{
				(int start, int length) = segment;

				// include the / before the segment
				// and subtract the offset to account for previously removed segments
				start -= 1 + offset;
				length++;

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

				Span<char> target = path[start..];

				// move the rest of the path to the left, overwriting the segment
				path[(start + length)..].CopyTo(target);

				// shorten the path
				path = path[..^length];

				offset += length;
			}

			if (path.Length > 1 && path[0] == '/')
			{
				path = path[1..];
			}
		}

		/// <summary>
		/// Sanitizes a string for use in log messages, by replacing all whitespace (including newlines, tabs, ...) with a single space.
		/// </summary>
		public static string ForLog(this string? input) => ForLog(input.AsSpan());

		public static string ForLog(this ReadOnlySpan<char> input)
		{
			if (input.IsEmpty)
			{
				return "";
			}
			var builder = new StringBuilder(input.Length);
			bool lastWasWhitespace = false;
			foreach (char c in input)
			{
				if (char.IsWhiteSpace(c))
				{
					if (!lastWasWhitespace)
					{
						builder.Append(' ');
						lastWasWhitespace = true;
					}
				} else
				{
					lastWasWhitespace = false;
					builder.Append(c);
				}
			}
			return builder.ToString();
		}

		public static void RemoveWhere<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, bool> predicate)
		{
			foreach ((TKey key, _) in dictionary.Where(predicate).ToList())
			{
				dictionary.Remove(key);
			}
		}
	}
}
