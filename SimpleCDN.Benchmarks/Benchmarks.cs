using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace SimpleCDN.Benchmarks
{
	//[SimpleJob(RuntimeMoniker.NativeAot90)]
	[SimpleJob(RuntimeMoniker.Net90)]
	[MemoryDiagnoser(false)]
	public class Benchmarks
	{
		public static IEnumerable<char[]> Paths =>
		[
			"/test.txt".ToCharArray(),
			"/data/data/.././data/../../../data/../data/test.json".ToCharArray(),
			"/data/data/../../data/../data/../data/../data/./../data/data/../data/../data/../data/./../data/test/data/../../test/data/../../data/../data/test.json".ToCharArray(),
		];

		const int NormalizationBenchmarkIterationsPerInvoke = 50;

		[Benchmark(OperationsPerInvoke = NormalizationBenchmarkIterationsPerInvoke)]
		[ArgumentsSource(nameof(Paths))]
		public long Normalize(char[] path)
		{
			long res = 0;

			for (var i = 0; i < NormalizationBenchmarkIterationsPerInvoke; i++)
			{
				var copy = path.ToArray();
				var span = copy.AsSpan();
				Helpers.Extensions.Normalize(ref span);
				res += span.BinarySearch('/');
			}

			return res;
		}
	}
}
