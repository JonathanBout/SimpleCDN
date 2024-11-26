using SimpleCDN.Helpers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SimpleCDN.Cache
{
	/// <summary>
	/// A cache that limits the total size of the stored values. When the size of the cache exceeds the specified limit, the oldest (least recently accessed) values are removed.
	/// </summary>
	/// <param name="maxSize">The maximum size of the cache, in bytes</param>
	/// <param name="comparer">The string comparer to use for the internal dictionary</param>
	internal class SizeLimitedCache(long maxSize, IEqualityComparer<string>? comparer)
	{
		public SizeLimitedCache(long maxSize) : this(maxSize, null) { }

		private readonly Dictionary<string, ValueWrapper> _dictionary = new(comparer);
		private readonly long _maxSize = maxSize;

		public CachedFile this[string key]
		{
			get => GetValue(key);
			set => SetValue(key, value);
		}

		public int Count => _dictionary.Count;

		public long Size => _dictionary.Values.Sum(wrapper => wrapper.Size);

		public bool TryGetValue(string key, [NotNullWhen(true)] out CachedFile? value)
		{
			if (_dictionary.TryGetValue(key, out ValueWrapper? valueWrapper))
			{
				value = valueWrapper.Value;
				return value is not null;
			}

			value = default;

			return false;
		}

		private void SetValue(string key, CachedFile value)
		{
			_dictionary[key] = new ValueWrapper(value);

			var byOldest = _dictionary.OrderBy(p => p.Value.AccessedAt).AsEnumerable();

			while (Size > _maxSize)
			{
				(var oldest, byOldest) = byOldest.RemoveFirst();

				_dictionary.Remove(oldest.Key);
			}
		}

		private CachedFile GetValue(string key)
		{
			if (_dictionary.TryGetValue(key, out var wrapper))
				return wrapper.Value;
			throw new KeyNotFoundException();
		}

		class ValueWrapper(CachedFile value)
		{
			private CachedFile _value = value;
			public CachedFile Value
			{
				get
				{
					AccessedAt = Stopwatch.GetTimestamp();
					return _value;
				}
				set
				{
					_value = value;
					AccessedAt = Stopwatch.GetTimestamp();
				}
			}
			public int Size => _value.Content.Length;
			public long AccessedAt { get; private set; } = Stopwatch.GetTimestamp();

			public static implicit operator CachedFile(ValueWrapper wrapper) => wrapper.Value;
		}
	}
}
