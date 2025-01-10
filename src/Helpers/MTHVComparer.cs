using Microsoft.Net.Http.Headers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleCDN.Helpers
{
	/// <summary>
	/// MediaTypeHeaderValue (MTHV) comparer that compares only the MediaType part of the header value.
	/// </summary>
	internal abstract class MTHVComparer : IEqualityComparer<MediaTypeHeaderValue>
	{
		public static MTHVComparer Instance { get; } = new MediaTypeHeaderValueComparerImpl();

		public abstract bool Equals(MediaTypeHeaderValue? x, MediaTypeHeaderValue? y);
		public abstract int GetHashCode([DisallowNull] MediaTypeHeaderValue obj);

		private class MediaTypeHeaderValueComparerImpl : MTHVComparer
		{
			public override bool Equals(MediaTypeHeaderValue? x, MediaTypeHeaderValue? y)
			{
				// if one or the other is null, but not both
				if (x is null)
				{
					return y is null;
				}

				if (y is null)
				{
					return false;
				}

				if (x.MediaType == "*/*" || y.MediaType == "*/*")
				{
					return true;
				}

				return x?.MediaType == y?.MediaType;
			}

			public override int GetHashCode([DisallowNull] MediaTypeHeaderValue obj)
			{
				return obj.MediaType.GetHashCode();
			}
		}

	}
}
