using Microsoft.Net.Http.Headers;
using System.Collections;

namespace SimpleCDN.Helpers
{
	internal static class EnumerableExtensions
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

		public static IEnumerator<T> JoinEnumerator<T>(this IEnumerator<T> source, IEnumerator<T> other)
		{
			if (source is MultipleEnumerator<T> multipleEnum)
			{
				// if the source is already a multiple enumerator, just add the other enumerator to it
				multipleEnum.Add(other);
				return multipleEnum;
			}

			return new MultipleEnumerator<T>(source, other);
		}

		public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
		{
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current;
			}
		}

		private class MultipleEnumerator<T>(params IEnumerable<IEnumerator<T>> enumerators) : IEnumerator<T>
		{
			private readonly IList<IEnumerator<T>> _enumerators = [..enumerators];

			private int _currentEnumerator = 0;

			public T Current => _enumerators[_currentEnumerator].Current;

			object IEnumerator.Current => Current!;

			public void Dispose()
			{
				foreach (IEnumerator<T> enumerator in _enumerators)
				{
					enumerator.Dispose();
				}
			}
			public bool MoveNext()
			{
				if (_enumerators[_currentEnumerator].MoveNext())
				{
					return true;
				} else if (_currentEnumerator < _enumerators.Count - 1)
				{
					_currentEnumerator++;
					return MoveNext();
				}
				return false;
			}

			public void Reset()
			{
				for (int i = _currentEnumerator; i >= 0; i--)
				{
					_enumerators[i].Reset();
				}
				_currentEnumerator = 0;
			}

			internal void Add(IEnumerator<T> enumerator)
			{
				_enumerators.Add(enumerator);
			}
		}
	}
}
