using Microsoft.Extensions.FileProviders;
using System.Collections;

namespace SimpleCDN.Tests.Mocks
{
	internal class MockDirectoryContents(string name) : IDirectoryContents
	{
		public bool Exists => true;
		public IEnumerator<IFileInfo> GetEnumerator()
		{
			yield return new MockFileInfo(name, "exists.txt");
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}