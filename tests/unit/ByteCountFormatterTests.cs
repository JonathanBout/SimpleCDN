﻿using SimpleCDN.Helpers;
using System.Globalization;

namespace SimpleCDN.Tests.Unit
{
	[TestFixture]
	public class ByteCountFormatterTests
	{
		[TestCase(0, "0B")]
		[TestCase(1, "1B")]
		[TestCase(1000, "1kB")]
		[TestCase(1024, "1.02kB")]
		[TestCase(1_000_000, "1MB")]
		[TestCase(1_000_000_000, "1GB")]
		[TestCase(1_000_000_000_000, "1TB")]
		[TestCase(-1, "-1B")]
		[TestCase(-1000, "-1kB")]
		[TestCase(1_000_000_000_000_000, "1PB")]
		[TestCase(1_000_000_000_000_000_000, "1EB")]
		[TestCase(long.MaxValue, "9.22EB")]
		[TestCase(long.MinValue, "-9.22EB")]
		public void TestByteCountFormatting(long input, string expectedOutput)
		{
			Assert.That(input.FormatByteCount(), Is.EqualTo(expectedOutput));
		}

		[TestCase(1010, "1,01kB", "nl-NL")]
		[TestCase(1010, "1.01kB", "en-US")]
		public void TestByteCountFormattingCultures(long input, string output, string culture)
		{
			Assert.That(input.FormatByteCount(CultureInfo.GetCultureInfo(culture)), Is.EqualTo(output));
		}
	}
}
