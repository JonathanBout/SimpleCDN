using SimpleCDN.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Tests.Unit
{
	public class CompressionAlgorithmSelectionTests
	{
		[Test]
		[TestCase(" DEFLATE")]
		[TestCase("gzip ")]
		[TestCase("bR   \t")]
		public void Test_CompressionAlgorithmFromName(string name)
		{
			Assert.That(CompressionAlgorithm.FromName(name), Is.Not.EqualTo(CompressionAlgorithm.None));
		}
	}
}
