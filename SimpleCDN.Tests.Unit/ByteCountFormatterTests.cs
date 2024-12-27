using SimpleCDN.Helpers;

namespace SimpleCDN.Tests.Unit
{
	[TestFixture(TestName = "Byte Count Formatter Tests")]
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
		[TestCase(1_000_000_000_000_000, "1000TB")]
		[TestCase(123_456_789_012_345, "123.46TB")]
		public void TestByteCountFormatting(long input, string expectedOutput)
		{
			Assert.That(input.FormatByteCount(), Is.EqualTo(expectedOutput));
		}
	}
}
