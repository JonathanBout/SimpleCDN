﻿namespace SimpleCDN.Tests.Unit
{
	[TestFixture]
	public class NormalizeTests
	{
		[TestCase("", "")]
		[TestCase("/", "/")]
		[TestCase("/./", "/")]
		[TestCase("/../", "/")]
		[TestCase("/a/", "a/")]
		[TestCase("/a/..", "")]
		[TestCase("/./a", "a")]
		[TestCase("/aaaa/../bbbb", "bbbb")]
		[TestCase("/aaaa/./bbbb", "aaaa/bbbb")]
		[TestCase("/aaaa/./bbbb/./cccc", "aaaa/bbbb/cccc")]
		[TestCase("/../aaaa/./bbbb/./cccc", "aaaa/bbbb/cccc")]
		[TestCase("/aaaa/./bbbb/./cccc/..", "aaaa/bbbb")]
		[TestCase("/aaaa/./bbbb/./cccc/../../dddd", "aaaa/dddd")]
		public void Test_Normalize_Normalizes(string path, string expected)
		{
			var pathChars = path.ToCharArray();
			Span<char> span = pathChars.AsSpan();

			Helpers.InternalExtensions.Normalize(ref span);

			Assert.That(span.ToString(), Is.EqualTo(expected));
		}
	}
}
