using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace SimpleCDN.Tests.Load
{
	static class Program
	{
		static async Task Main(string[] args)
		{
			Console.TreatControlCAsInput = true;
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

			HttpClient[] clients = [.. Enumerable.Range(0, requestsPerSecond)
			.Select(_ => new HttpClient
			{
				BaseAddress = uriResult,
				DefaultRequestHeaders = {
					{ "User-Agent", "SimpleCDN Load Testing" }
				}
			})];

			ConcurrentBag<int> durations = [];

			while (!Console.KeyAvailable || Console.ReadKey() is not { Key: ConsoleKey.C, Modifiers: ConsoleModifiers.Control })
			{
				var start = Stopwatch.GetTimestamp();
				await Parallel.ForAsync(0, requestsPerSecond, async (i, ct) =>
				{
					var start = Stopwatch.GetTimestamp();
					await SendRequest(clients[i], ct);
					durations.Add((int)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
				});
				TimeSpan elapsed = Stopwatch.GetElapsedTime(start);

				Console.WriteLine("Sent {0} requests in {1:0.##} ms", requestsPerSecond, elapsed.TotalMilliseconds);

				if (elapsed.TotalMilliseconds < 1000)
				{
					await Task.Delay(1000 - (int)elapsed.TotalMilliseconds);
				}
			}

			Console.WriteLine();
			Console.WriteLine("Average request duration: {0:0.##} ms", durations.Average());
			Console.WriteLine("Max request duration: {0:0.##} ms", durations.Max());
			Console.WriteLine("Min request duration: {0:0.##} ms", durations.Min());
			Console.WriteLine("Total requests: {0}", durations.Count);
			Console.WriteLine();
			var percentiles = new[] { 0.5, 0.75, 0.9, 0.95, 0.99 };
			foreach (var percentile in percentiles)
			{
				Console.WriteLine("{0}th percentile: {1:0.##} ms", percentile * 100, durations.Percentile(percentile));
			}
		}

		static double Percentile(this ConcurrentBag<int> durations, double percentile)
		{
			var sorted = durations.Order().ToArray();
			int index = (int)(percentile * sorted.Length);
			return sorted[index];
		}

		static async Task SendRequest(HttpClient client, CancellationToken ct)
		{
			try
			{
				HttpResponseMessage response = await client.GetAsync("", ct);

				if (!response.IsSuccessStatusCode)
					Console.WriteLine($"Request failed: {response.StatusCode}");

			} catch (Exception ex)
			{
				Console.WriteLine($"Request failed: {ex.Message}");
			}
		}
	}
}
