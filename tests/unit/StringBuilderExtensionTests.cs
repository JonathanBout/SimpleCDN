using SimpleCDN.Helpers;
using System.Text;

namespace SimpleCDN.Tests.Unit
{
	public class StringBuilderExtensionTests
	{
		public record TestInput(string[] Parts, (string, object[])[] formattedParts, Encoding Encoding)
		{
			public override string ToString() => $"{Parts.Length} parts, {Encoding.EncodingName}";
		}

		private static TestInput[] ToByteArray_HasSameResultAs_Manual_TestCases()
		{
			(string, object[])[] formattedParts = [("Test {0}", [new { aa = "bb" }])];
			string[] shortArray = ["Hello, ", "world!"];
			string[] longArray = ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j",
				"k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"];

			return [
				new TestInput(shortArray, [], Encoding.UTF8),
				new TestInput(shortArray, [], Encoding.Unicode),
				new TestInput(shortArray, [], Encoding.ASCII),
				new TestInput(shortArray, [], Encoding.UTF32),
				new TestInput(longArray, formattedParts, Encoding.UTF8),
				new TestInput(longArray, formattedParts, Encoding.Unicode),
				new TestInput(longArray, formattedParts, Encoding.ASCII),
				new TestInput(longArray, formattedParts, Encoding.UTF32)
				];
		}

		[Test]
		[TestCaseSource(nameof(ToByteArray_HasSameResultAs_Manual_TestCases))]
		public void ToByteArray_HasSameResultAs_Manual(TestInput input)
		{
			(string[] parts, (string, object[])[] formattedParts, Encoding encoding) = input;

			var sb = new StringBuilder();
			foreach (var part in parts)
			{
				sb.Append(part);
			}

			foreach ((string format, object[] args) in formattedParts)
			{
				sb.AppendFormat(format, args);
			}

			var manual = sb.ToByteArray(encoding);
			var builtIn = encoding.GetBytes(sb.ToString());

			Assert.That(manual, Is.EqualTo(builtIn));
		}
	}
}
