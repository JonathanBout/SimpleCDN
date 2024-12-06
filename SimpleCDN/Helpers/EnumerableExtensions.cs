

using Microsoft.Net.Http.Headers;

namespace SimpleCDN.Helpers
{
	public static class EnumerableExtensions
	{
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

		public static bool ContainsMediaType(this IList<MediaTypeHeaderValue> list, string mediaType)
		{
			foreach (var item in list)
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
