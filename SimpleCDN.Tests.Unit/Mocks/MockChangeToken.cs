using Microsoft.Extensions.Primitives;
using SimpleCDN.Tests.Unit.Mocks;

namespace SimpleCDN.Tests.Unit.Mocks
{
	internal class MockChangeToken : IChangeToken
	{
		public bool HasChanged { get; }
		public bool ActiveChangeCallbacks { get; }
		public IDisposable RegisterChangeCallback(Action<object> callback, object? state)
		{
			return new MockDisposable();
		}
	}
}
