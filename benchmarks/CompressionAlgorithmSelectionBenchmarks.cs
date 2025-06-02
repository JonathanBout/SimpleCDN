using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using SimpleCDN.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Benchmarks
{
	[MemoryDiagnoser(false)]
	[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
	[SimpleJob(RuntimeMoniker.Net80)]
	public class CompressionAlgorithmSelectionBenchmarks
	{
		[Benchmark]
		[Arguments(" gzip")]
		[Arguments("DEFLATE   ")]
		[Arguments("      \t BR\t\t")]
		public CompressionAlgorithm SelectFromName(string name)
		{
			return CompressionAlgorithm.FromName(name);
		}
	}
}
