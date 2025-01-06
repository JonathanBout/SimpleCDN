using Microsoft.Net.Http.Headers;

namespace SimpleCDN.Helpers
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Removes the first element from the enumerable and returns it along with the rest of the enumerable.
		/// </summary>
		/// <returns>The first element of <paramref name="source"/></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static (T left, IEnumerable<T> right) RemoveFirst<T>(this IEnumerable<T> source)
		{
			using IEnumerator<T> enumerator = source.GetEnumerator();

			if (!enumerator.MoveNext())
				throw new ArgumentOutOfRangeException(nameof(source));

			return (enumerator.Current, RestEnumerator(enumerator));

			static IEnumerable<T> RestEnumerator(IEnumerator<T> enumerator)
			{
				while (enumerator.MoveNext())
				{
					yield return enumerator.Current;
				}
			}
		}

		/// <summary>
		/// Compares the media type of the <see cref="MediaTypeHeaderValue"/> to the provided media type string.
		/// </summary>
		public static bool ContainsMediaType(this IList<MediaTypeHeaderValue> list, string mediaType)
		{
			foreach (MediaTypeHeaderValue item in list)
			{
				if (MTHVComparer.Instance.Equals(item, new MediaTypeHeaderValue(mediaType)))
				{
					return true;
				}
			}
			return false;
		}
	}
}
