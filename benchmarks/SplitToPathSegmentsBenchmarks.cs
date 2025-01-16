﻿using BenchmarkDotNet.Attributes;
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
	public class SplitToPathSegmentsBenchmarks
	{
		public string[] Paths { get; } = [
				"/a/very/long/path/../path/with/./segments/and/..",
				"/a/short/path",
				"empty"
			];

		[Benchmark]
		[ArgumentsSource(nameof(Paths))]
		public ICollection<Range> SplitToPathSegments(ReadOnlySpan<char> path)
		{
			return path.SplitToPathSegments();
		}
	}
}