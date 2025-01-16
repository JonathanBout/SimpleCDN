namespace SimpleCDN.Helpers
{
	internal static class SpanExtensions
	{
		public static IEnumerable<Range> SplitToPathSegments(this Span<char> path, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
		{
			return SplitToPathSegments(path.AsReadOnly(), options);
		}

		public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> span) => span;

		public static ICollection<Range> SplitToPathSegments(this ReadOnlySpan<char> path, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
		{
			bool trim = (options & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries;
			bool removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) == StringSplitOptions.RemoveEmptyEntries;

			int start = 0;

			ICollection<Range> result = [];

			for (int i = 0; i < path.Length; i++)
			{
				if (path[i] == Path.DirectorySeparatorChar || path[i] == Path.AltDirectorySeparatorChar)
				{
					// equivalent of StringSplitOptions.RemoveEmptyEntries
					if (i == start && removeEmpty)
					{
						start = i + 1;
						continue;
					}

					if (trim)
					{
						result.Add(path.TrimmedRange(start, i));
					} else
					{
						result.Add(new Range(start, i));
					}
					start = i + 1;
				}
			}

			if (start < path.Length)
			{
				result.Add(path.TrimmedRange(start, path.Length));
			}

			return result;
		}

		/// <summary>
		/// Returns a new range from <paramref name="start"/> to <paramref name="end"/> with leading and trailing whitespace removed.
		/// </summary>
		private static Range TrimmedRange(this ReadOnlySpan<char> span, int start, int end)
		{
			while (start < end && char.IsWhiteSpace(span[start]))
			{
				start++;
			}

			while (end > start && char.IsWhiteSpace(span[end - 1]))
			{
				end--;
			}

			return new Range(start, end);
		}
	}
}
