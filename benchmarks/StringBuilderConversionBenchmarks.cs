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
			StringBuilder longStringBuilder = new();
			for (int i = 0; i < 1000; i++)
			{
				longStringBuilder.AppendFormat("Test {0}: ", i).Append(random.GetItems<char>("abcdefghijklmnopqrstuvwxyz", 10));
			}

			StringBuilder shortStringBuilder = new();
			for (int i = 0; i < 10; i++)
			{
				shortStringBuilder.AppendFormat("Test {0}: ", i).Append(random.GetItems<char>("abcdefghijklmnopqrstuvwxyz", 10));
			}
			return [longStringBuilder, shortStringBuilder];
		}

		public Encoding[] GetEncodings()
		{
			return [Encoding.UTF8, Encoding.ASCII];
		}

		[ParamsSource(nameof(GetEncodings))]
		public Encoding CurrentEncoding { get; set; } = Encoding.UTF8;

		[ArgumentsSource(nameof(GetData))]
		[Benchmark]
		public byte[] Manual(StringBuilder sb)
		{
			return sb.ToByteArray(CurrentEncoding);
		}

		[ArgumentsSource(nameof(GetData))]
		[Benchmark]
		public byte[] BuiltIn(StringBuilder sb)
		{
			return CurrentEncoding.GetBytes(sb.ToString());
		}
}
}
