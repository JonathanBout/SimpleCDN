using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Benchmarks
{
	public class Benchmarks
	{
		const int NormalizationBenchmarkIterationsPerInvoke = 100;
		[Benchmark(OperationsPerInvoke = NormalizationBenchmarkIterationsPerInvoke)]
		public void NormalizationBenchmark()
		{
			var path = "/a/b/c/../d/e/f/./d/e/a/../../q/r/s/t/u/./v/w/x/y/z".ToCharArray();
			var span = path.AsSpan();
			for (var i = 0; i < NormalizationBenchmarkIterationsPerInvoke; i++)
			{
				Helpers.Extensions.Normalize(ref span);
			}
		}
	}
}
