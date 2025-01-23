using SimpleCDN.Services;
using System.Text;

namespace SimpleCDN.Tests.Mocks
{
	public class MockIndexGenerator : IIndexGenerator
	{
		public byte[]? GenerateIndex(string absolutePath, string rootRelativePath)
		{
			return Encoding.UTF8.GetBytes("Mock index");
		}
	}
}
