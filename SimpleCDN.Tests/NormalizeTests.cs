﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Tests
{
	[TestFixture]
	public class NormalizeTests
	{
		[TestCase("/aaaa/../bbbb", "/bbbb")]
		[TestCase("/aaaa/./bbbb", "/aaaa/bbbb")]
		[TestCase("/aaaa/./bbbb/./cccc", "/aaaa/bbbb/cccc")]
		[TestCase("/../aaaa/./bbbb/./cccc", "/aaaa/bbbb/cccc")]
		[TestCase("/aaaa/./bbbb/./cccc/..", "/aaaa/bbbb")]
		[TestCase("/aaaa/./bbbb/./cccc/../../dddd", "/aaaa/dddd")]
		public void Test_Normalize_Normalizes(string path, string expected)
		{
			var pathChars = path.ToCharArray();
			var span = pathChars.AsSpan();

			Helpers.Extensions.Normalize(ref span);

			Assert.That(span.ToString(), Is.EqualTo(expected));
		}
	}
}