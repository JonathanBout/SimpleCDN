namespace SimpleCDN.Extensions.Redis
{
	/// <summary>
	/// Balances access to a collection of instances of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="instanceFactory">How an instance should be created.</param>
	/// <param name="healthCheck">A function that determines if an instance is healthy.</param>
	internal sealed class ObjectAccessBalancer<T>(Func<T> instanceFactory, Func<T, bool>? healthCheck = null) : IDisposable, IAsyncDisposable
	{
		private readonly List<T> _instances = [];
		private T? _lastDeactivatedInstance;
		private readonly Func<T> _instanceFactory = instanceFactory;
		private readonly Func<T, bool>? _healthCheck = healthCheck;

		private int _index;
#if NET9_0_OR_GREATER
		private readonly Lock _lock = new();
#else
		private readonly object _lock = new();
#endif
		/// <summary>
		/// The number of instances currently in use in the balancer.
		/// </summary>
		public int Count => _instances.Count;

		/// <summary>
		/// The highest number of instances that have been in use in the balancer at the same time.
		/// </summary>
		public int HighestCount { get; private set; } = 1;

		/// <summary>
		/// Gets the next instance in the balancer.
		/// </summary>
		public T Next()
		{
			lock (_lock)
			{
				// if there is no health check, just return the next instance
				if (_healthCheck is null)
				{
					if (++_index >= _instances.Count)
						_index = 0;
					return _instances[_index];
				}

				// if there is a health check, find the next healthy instance
				T? instance = default;
				var startIndex = _index;
				while (_instances.Count > 0)
				{
					if (++_index >= _instances.Count)
						_index = 0;

					instance = _instances[_index];

					if (!_healthCheck.Invoke(instance))
					{
						// if the instance is unhealthy, Dispose it if it implements IDisposable,
						// and remove it from the list
						if (instance is IDisposable disposable)
							disposable.Dispose();
						if (instance is IAsyncDisposable asyncDisposable)
							asyncDisposable.DisposeAsync().AsTask();
						_instances.RemoveAt(_index);
					} else
					{
						break;
					}
				}

				if (instance is null || instance.Equals(default(T)) || _instances.Count == 0)
				{
					// all instances are unhealthy, clear the list and add a new instance
					instance = AddInstance();
				}

				return instance;
			}
		}

		/// <summary>
		/// Adds a new instance to the balancer.
		/// </summary>
		public T AddInstance()
		{
			lock (_lock)
			{
				if (_lastDeactivatedInstance is not null)
				{
					_instances.Add(_lastDeactivatedInstance);
					_lastDeactivatedInstance = default;
				} else
				{
					_instances.Add(_instanceFactory());
				}

				if (_instances.Count > HighestCount)
					HighestCount = _instances.Count;
				return _instances[^1];
			}
		}

		/// <summary>
		/// Removes the last instance added to the balancer, unless there is only one instance.
		/// The instance is not removed completely until you call this method again.
		/// Until then, calling <see cref="AddInstance"/> will re-add the removed instance.
		/// </summary>
		public void RemoveOneInstance()
		{
			lock (_lock)
			{
				if (_instances.Count > 0)
				{
					if (_lastDeactivatedInstance is IDisposable last)
						last.Dispose();
					_lastDeactivatedInstance = _instances[^1];
					_instances.RemoveAt(_instances.Count - 1);
				}
			}
		}

		/// <summary>
		/// If <typeparamref name="T"/> implements <see cref="IDisposable"/>, disposes all instances.
		/// <br/>
		/// You do not need to call this method if your instances do not implement <see cref="IDisposable"/>.
		/// </summary>
		public void Dispose()
		{
			if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
			{
				foreach (T instance in _instances)
				{
					(instance as IDisposable)?.Dispose();
				}
				if (_lastDeactivatedInstance is IDisposable deactivated)
					deactivated.Dispose();
			}
		}

		/// <summary>
		/// If <typeparamref name="T"/> implements <see cref="IAsyncDisposable"/>, disposes all instances asynchronously.
		/// Also, if <typeparamref name="T"/> implements <see cref="IDisposable"/>, disposes all instances synchronously.
		/// <br/>
		/// You do not need to call this method if your instances do not implement <see cref="IAsyncDisposable"/>.
		/// </summary>
		public async ValueTask DisposeAsync()
		{
			if (typeof(IAsyncDisposable).IsAssignableFrom(typeof(T)))
			{
				var tasks = new List<Task>();
				foreach (T instance in _instances)
				{
					if (instance is IAsyncDisposable asyncDisposable)
						tasks.Add(asyncDisposable.DisposeAsync().AsTask());
				}
				if (_lastDeactivatedInstance is IAsyncDisposable asyncDeactivated)
					tasks.Add(asyncDeactivated.DisposeAsync().AsTask());

				await Task.WhenAll(tasks);
			}

			Dispose();
		}
	}
}
