using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NaughtyStrings;
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

		[Test]
		public void SplitPathSegments_NaughthyStrings_StillWorks()
		{
			for (int i = 0; i < TheNaughtyStrings.All.Count - 2; i++)
			{
				if (TheNaughtyStrings.All[i].Contains('/') || TheNaughtyStrings.All[i + 1].Contains('/')
				 || TheNaughtyStrings.All[i].Contains('\\') || TheNaughtyStrings.All[i + 1].Contains('\\')
				 || string.IsNullOrWhiteSpace(TheNaughtyStrings.All[i]) || string.IsNullOrWhiteSpace(TheNaughtyStrings.All[i + 1]))
				{
					continue;
				}

				var path = TheNaughtyStrings.All[i].Trim() + "/" + TheNaughtyStrings.All[i + 1];

				ReadOnlySpan<char> span = path;

				Range[] result = [.. span.SplitToPathSegments()];

				Assert.That(result, Has.Length.EqualTo(2), $"Path: '{path}' of length {path.Length}");
				Assert.Multiple(() =>
				{
					Assert.That(path[result[0]], Is.EqualTo(TheNaughtyStrings.All[i].Trim()));
					Assert.That(path[result[1]], Is.EqualTo(TheNaughtyStrings.All[i + 1].Trim()));
				});
			}
		}
	}
}
