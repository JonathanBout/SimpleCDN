using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections;

namespace SimpleCDN.Tests.Load
{
	static class Program
	{
		internal static readonly double[] percentiles = [0.5, 0.75, 0.9, 0.95, 0.99];

		static readonly Lock _consoleLock = new();

		static async Task Main(string[] args)
		{
			Console.Clear();
			if (args.Length != 2)
			{
				Console.WriteLine("Usage: SimpleCDN.Tests.Load <url> <requests per second>");
				return;
			}

			var url = args[0];
			if (!int.TryParse(args[1], out var requestsPerSecond))
			{
				Console.WriteLine("Invalid number of requests.");
				return;
			}

			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult))
			{
				Console.WriteLine("Invalid URL.");
				return;
			}

			HttpClient CreateClient() => new()
			{
				BaseAddress = uriResult,
				DefaultRequestHeaders = {
					{ "User-Agent", "SimpleCDN Load Testing" }
				}
			};

			ConcurrentBag<int> durations = [];
			var start = Stopwatch.GetTimestamp();
			long errors = 0;

			DateTimeOffset lastRender = DateTimeOffset.MinValue;

			(int left, int top) = Console.GetCursorPosition();

			Render();

			void Render()
			{
				if (DateTimeOffset.Now - lastRender > TimeSpan.FromSeconds(2))
				{
					lastRender = DateTimeOffset.Now;
				} else
				{
					return;
				}

				TimeSpan elapsed = Stopwatch.GetElapsedTime(start);

				lock (_consoleLock)
				{
					Console.SetCursorPosition(left, top);
					Console.WriteLine("Average request duration: {0:0.##} ms          ", durations.Average());
					Console.WriteLine("Max request duration: {0:0.##} ms          ", durations.Max());
					Console.WriteLine("Min request duration: {0:0.##} ms          ", durations.Min());
					Console.WriteLine("Total requests: {0}          ", durations.Count);
					Console.WriteLine("Failed requests: {0}          ", errors);
					Console.WriteLine("                                                                    ");

					foreach (var percentile in percentiles)
					{
						Console.WriteLine("{0}th percentile: {1:0.##} ms          ", percentile * 100, durations.Percentile(percentile));
					}

					Console.WriteLine(Environment.NewLine + Environment.NewLine);
				}
			}

			await Parallel.ForEachAsync(new InfiniteEnumerable(), async (_, ct) =>
			{
				using HttpClient client = CreateClient();
				var start = Stopwatch.GetTimestamp();
				if (!await SendRequest(client, ct))
				{
					Interlocked.Increment(ref errors);
				}
				durations.Add((int)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
				Render();
			});
		}

		static double Percentile(this ConcurrentBag<int> durations, double percentile)
		{
			var sorted = durations.Order().ToArray();
			int index = (int)(percentile * sorted.Length);
			return sorted[index];
		}

		static async Task<bool> SendRequest(HttpClient client, CancellationToken ct)
		{
			try
			{
				using HttpResponseMessage response = await client.GetAsync("", ct);

				if (!response.IsSuccessStatusCode)
				{
					Console.WriteLine($"Request failed: {response.StatusCode}");
					return false;
				}
			} catch (Exception ex)
			{
				Console.WriteLine($"Request failed: {ex.Message}");
				return false;
			}
			return true;
		}

		private class InfiniteEnumerable : IEnumerable<ulong>
		{
			public IEnumerator<ulong> GetEnumerator() => new InfiniteEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		private class InfiniteEnumerator : IEnumerator<ulong>
		{
			private ulong _current;
			public ulong Current => _current;

			object IEnumerator.Current => Current;

			public void Dispose() { }
			public bool MoveNext()
			{
				Interlocked.Increment(ref _current);
				return true;
			}
			public void Reset() => _current = 0;
		}
	}
}
