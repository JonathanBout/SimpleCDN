using SimpleCDN.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Tests.Unit
{
	public class StringBuilderExtensionTests
	{
		public record TestInput(string[] Parts, Encoding Encoding)
		{
			public override string ToString() => $"{Parts.Length} parts, {Encoding.EncodingName}";
		}

		internal static TestInput[] ToByteArray_HasSameResultAs_Manual_TestCases()
		{
			return [
				new TestInput(["Hello, ", "world!"], Encoding.UTF8),
				new TestInput(["Hello, ", "world!"], Encoding.Unicode),
				new TestInput(["Hello, ", "world!"], Encoding.ASCII),
				new TestInput(["Hello, ", "world!"], Encoding.UTF32),
				new TestInput(["a", "b", "c", "d", "e", "f", "g", "h", "i", "j"], Encoding.UTF8),
				new TestInput(["a", "b", "c", "d", "e", "f", "g", "h", "i", "j"], Encoding.Unicode),
				new TestInput(["a", "b", "c", "d", "e", "f", "g", "h", "i", "j"], Encoding.ASCII),
				new TestInput(["a", "b", "c", "d", "e", "f", "g", "h", "i", "j"], Encoding.UTF32)
				];
		}

		[Test]
		[TestCaseSource(nameof(ToByteArray_HasSameResultAs_Manual_TestCases))]
		public void ToByteArray_HasSameResultAs_Manual(TestInput input)
		{
			(string[] parts, Encoding encoding) = input;

			var sb = new StringBuilder();
			foreach (var part in parts)
			{
				sb.Append(part);
			}

			var manual = sb.ToByteArray(encoding);
			var builtIn = encoding.GetBytes(sb.ToString());

			Assert.That(manual, Is.EqualTo(builtIn));
		}
	}
}
