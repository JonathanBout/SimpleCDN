using SimpleCDN.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleCDN.Tests.Mocks
{
	public class MockCDNContext : ICDNContext
	{
		public string BaseUrl => "/";
		public string GetSystemFilePath(string filename) => filename;
	}
}
