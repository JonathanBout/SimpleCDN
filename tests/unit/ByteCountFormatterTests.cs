using SimpleCDN.Helpers;
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
		[TestCase(long.MinValue, "-9,22EB", "nl-NL")]
		public void TestByteCountFormatting(long input, string expectedOutput, string? culture = null)
		{
			CultureInfo targetCulture = culture is null ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture);

			Assert.That(input.FormatByteCount(targetCulture), Is.EqualTo(expectedOutput));
		}
	}
}
