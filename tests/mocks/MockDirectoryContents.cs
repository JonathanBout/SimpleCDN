using Microsoft.Extensions.FileProviders;
using SimpleCDN.Tests.Mocks;
using System.Collections;

namespace SimpleCDN.Tests.Mocks
{
	public class MockDirectoryContents(string name) : IDirectoryContents
	{
		public bool Exists => true;
		public IEnumerator<IFileInfo> GetEnumerator()
		{
			yield return new MockFileInfo(name, "exists.txt");
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
