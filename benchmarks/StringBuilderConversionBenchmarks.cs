using BenchmarkDotNet.Attributes;
using SimpleCDN.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Benchmarks
{
	[MemoryDiagnoser(false)]
	public class StringBuilderConversionBenchmarks
	{
		public StringBuilder[] GetData()
		{
			Random random = new(293);
			StringBuilder sb = new();
			for (int i = 0; i < 1000; i++)
			{
				sb.Append(i).Append(random.GetItems<char>("abcdefghijklmnopqrstuvwxyz", 10));
			}
			return [sb];
		}

		[ArgumentsSource(nameof(GetData))]
		[Benchmark]
		public byte[] Manual(StringBuilder sb)
		{
			return sb.ToByteArray(Encoding.UTF8);
		}

		[ArgumentsSource(nameof(GetData))]
		[Benchmark]
		public byte[] BuiltIn(StringBuilder sb)
		{
			return Encoding.UTF8.GetBytes(sb.ToString());
		}
	}
}
