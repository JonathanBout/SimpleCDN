using Microsoft.Extensions.Primitives;

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
