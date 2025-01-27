using SimpleCDN.Helpers;
using System.Diagnostics;
using System.IO.Pipes;

namespace SimpleCDN.Services.Caching.Implementations
{
	#region old
	//   internal partial class InMemoryCache : IHostedService
	//{
	//	#region Automated Purging
	//	private CancellationTokenSource? _backgroundCTS;
	//	private IDisposable? _optionsOnChange;

	//	/// <summary>
	//	/// Notifies the cache that there are new items that may need to be purged.
	//	/// </summary>
	//	private void Purge()
	//	{
	//		_dictionary.RemoveWhere(kvp => Stopwatch.GetElapsedTime(kvp.Value.AccessedAt) < _cacheOptions.CurrentValue.MaxAge);
	//	}

	//	private async Task PurgeLoop()
	//	{
	//		while (_backgroundCTS?.Token.IsCancellationRequested is false)
	//		{
	//			await Task.Delay(_options.CurrentValue.PurgeInterval, _backgroundCTS.Token);

	//			if (_backgroundCTS.Token.IsCancellationRequested)
	//				break;

	//			_logger.LogDebug("Purging expired cache items");

	//			Purge();
	//		}
	//	}

	//	public Task StartAsync(CancellationToken cancellationToken)
	//	{
	//		// register the options change event to restart the background task
	//		_optionsOnChange ??= _options.OnChange((_,_) => StartAsync(default));

	//		if (_cacheOptions.CurrentValue.MaxAge == TimeSpan.Zero || _options.CurrentValue.PurgeInterval == TimeSpan.Zero)
	//		{
	//			// automatic expiration and purging are disabled.
	//			// stop the background task if it's running and return
	//			_backgroundCTS?.Dispose();
	//			_backgroundCTS = null;
	//			return Task.CompletedTask;
	//		}

	//		if (_backgroundCTS is not null)
	//		{
	//			// background task is already running, no need to start another
	//			return Task.CompletedTask;
	//		}

	//		_backgroundCTS = new CancellationTokenSource();

	//		_backgroundCTS.Token.Register(() =>
	//		{
	//			// if the token is cancelled, dispose the token source and set it to null
	//			// so that the next time StartAsync is called it may be recreated
	//			_backgroundCTS?.Dispose();
	//			_backgroundCTS = null;
	//		});

	//		// The background task will run in the background until the token is cancelled
	//		// execution of the current method will continue immediately
	//		Task.Run(PurgeLoop,
	//			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _backgroundCTS.Token).Token);

	//		return Task.CompletedTask;
	//	}

	//	public Task StopAsync(CancellationToken cancellationToken)
	//	{
	//		_optionsOnChange?.Dispose();
	//		return _backgroundCTS?.CancelAsync() ?? Task.CompletedTask;
	//	}
	//	#endregion
	//}
	#endregion

	partial class InMemoryCache
	{
		private CancellationTokenSource? _purgeCTS;

		private Task? _purgeTask;

		/// <summary>
		/// Notifies the cache that there are new items that may need to be purged in the future.
		/// </summary>
		private void Purge()
		{
			if (_purgeTask is not { IsCompleted: false })
			{
				// of the task is not running anymore or the token is cancelled, restart the task
				_purgeCTS?.Cancel();
				_purgeCTS?.Dispose();
				_purgeTask?.Dispose();
				_purgeCTS = new CancellationTokenSource();
				_purgeTask = Task.Run(PurgeLoop, _purgeCTS.Token);
			}
		}

		private async Task PurgeLoop()
		{
			while (_purgeCTS is { IsCancellationRequested: false })
			{
				// if there are no items in the cache, there is no need to purge
				// so we stop the background task until there are new items.
				// This is to save resources and prevent unnecessary loops and checks.
				if (_dictionary.IsEmpty)
				{
					return;
				}

				TimeSpan oldestAge = Stopwatch.GetElapsedTime(_dictionary.MinBy(kvp => kvp.Value.AccessedAt).Value.AccessedAt);

				if (oldestAge > _cacheOptions.CurrentValue.MaxAge)
				{
					ActualPurge();
					continue;
				}

				TimeSpan timeUntilNextPurge = _cacheOptions.CurrentValue.MaxAge - oldestAge;

				await Task.Delay(timeUntilNextPurge, _purgeCTS.Token);

				if (_purgeCTS.Token.IsCancellationRequested)
					break;

				ActualPurge();
			}
		}

		private void ActualPurge()
		{
			var now = Stopwatch.GetTimestamp();
			_dictionary.RemoveWhere(kvp => Stopwatch.GetElapsedTime(kvp.Value.AccessedAt, now) > _cacheOptions.CurrentValue.MaxAge);
		}

		private void DisposePurging()
		{
			_purgeTask?.Dispose();
		}
	}
}
