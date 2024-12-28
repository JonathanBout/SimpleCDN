using Microsoft.Extensions.Primitives;
using SimpleCDN.Tests.Mocks;

namespace SimpleCDN.Tests.Mocks
{
	public class MockChangeToken : IChangeToken
	{
		public bool HasChanged { get; }
		public bool ActiveChangeCallbacks { get; }
		public IDisposable RegisterChangeCallback(Action<object> callback, object? state)
		{
			return new MockDisposable();
		}
	}
}
