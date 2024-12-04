using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Benchmarks
{
	[MemoryDiagnoser(false)]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Needed for BenchmarkDotNet")]
	public class Benchmarks
	{
		public IEnumerable<char[]> Paths =>
		[
			"/".ToCharArray(),
			"/../".ToCharArray(),
			"/data/../".ToCharArray(),
			"/test.txt".ToCharArray(),
			"/data/../test.txt".ToCharArray(),
			"/data/test.json".ToCharArray(),
			"/data/../data/test.json".ToCharArray(),
			"/data/./../data/test.json".ToCharArray(),
			"/data/data/../../data/test.json".ToCharArray(),
			"/data/data/../../data/../data/test.json".ToCharArray(),
			"/data/data/../../data/../data/../data/test.json".ToCharArray(),
			"/data/data/.././data/../../../data/../data/test.json".ToCharArray(),
		];

		[ParamsSource(nameof(Paths))]
		public char[] Path { get; set; } = [];

		const int NormalizationBenchmarkIterationsPerInvoke = 100;

		[Benchmark(OperationsPerInvoke = NormalizationBenchmarkIterationsPerInvoke)]
		public void NormalizationBenchmark()
		{
			var span = Path.AsSpan();
			for (var i = 0; i < NormalizationBenchmarkIterationsPerInvoke; i++)
			{
				Helpers.Extensions.Normalize(ref span);
			}
		}
	}
}
