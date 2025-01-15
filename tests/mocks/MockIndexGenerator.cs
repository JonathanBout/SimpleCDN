using SimpleCDN.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
