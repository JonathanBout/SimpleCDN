using SimpleCDN.Helpers;

namespace SimpleCDN.Tests.Unit
{
	public class SpanExtensionsTests
	{
		private static readonly (string, int)[] testPaths = [
				("path/to/file", 3),
				("/other/path/to/file", 4),
				("file", 1),
				("/file", 1),
				("/", 0),
				("", 0),
			];

		[Test]
		[TestCaseSource(nameof(testPaths))]
		public void SplitPathSegments_FindsAllSegments_DoesNotIncludeSeparator((string path, int expectedPartCount) testData)
		{
			(string path, int expectedPartCount) = testData;

			ReadOnlySpan<char> span = path;

			IReadOnlyCollection<Range> result = [.. span.SplitToPathSegments()];

			Assert.That(result, Has.Count.EqualTo(expectedPartCount));

			// use the ranges to extract the segments
			var resultStrings = result
				.Select(r => r.GetOffsetAndLength(path.Length))
				.Select(r => path.Substring(r.Offset, r.Length))
				.ToList();

			Assert.That(resultStrings, Has.None.Contain('/'));
		}
	}
}
