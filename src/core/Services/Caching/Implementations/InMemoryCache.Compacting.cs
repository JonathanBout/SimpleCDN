using SimpleCDN.Helpers;

namespace SimpleCDN.Services.Caching.Implementations
{
	partial class InMemoryCache
	{
		private bool _newItemsAdded = false;
		private Task? _compactingTask;

		private readonly CancellationTokenSource _compactingCTS = new();

		private void DisposeCompacting()
		{
			_compactingCTS.Cancel();
			// Wait 10 ms for the task to finish gracefully
			_compactingTask?.Wait(10);
			_compactingCTS.Dispose();
		}

		/// <summary>
		/// Notifies the background task that there may be work to do.
		/// </summary>
		private void Compact()
		{
			_newItemsAdded = true;
			if (_compactingTask is not { IsCompleted: false })
			{
				_compactingTask?.Dispose();
				_compactingTask = Task.Run(CompactBackgroundTask);
			}
		}

		/// <summary>
		/// Removes the oldest (least recently accessed) items from the cache until the size is within the limit.
		/// Do not use this method directly, use <see cref="Compact"/> instead.
		/// </summary>
		void CompactBackgroundTask()
		{
			while (_newItemsAdded && !_compactingCTS.IsCancellationRequested && Size > MaxSize)
			{
				_newItemsAdded = false;

				using IEnumerator<KeyValuePair<string, ValueWrapper>> byOldestEnumerator =
					_dictionary
						.OrderBy(wrapper => wrapper.Value.AccessedAt).GetEnumerator();

				// remove the oldest items until the size is within the limit
				while (!_compactingCTS.IsCancellationRequested && Size > MaxSize)
				{
					try
					{
						byOldestEnumerator.MoveNext();
						_dictionary.TryRemove(byOldestEnumerator.Current.Key, out _);
					} catch (ArgumentOutOfRangeException)
					{
						// code should never reach this point, but just in case as a safety net
						_logger.LogWarning("Cache size exceeded the limit, but no more items could be removed to bring it back within the limit.");
						break;
					}
				}
			}
		}
	}
}
