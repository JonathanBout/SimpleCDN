using System.Numerics;

namespace SimpleCDN.Helpers
{
	public static class Extensions
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
	}
}
