using Microsoft.Extensions.Options;

namespace SimpleCDN.Tests
{
	internal class OptionsMock<T>(T value) : IOptionsMonitor<T>, IOptions<T>, IOptionsSnapshot<T> where T : class
	{
		public T CurrentValue => value;
		public T Value => value;

		public T Get(string? name) => value;
		public IDisposable? OnChange(Action<T, string?> listener) => new MockDisposable();
	}

	internal class MockDisposable : IDisposable
	{
		public void Dispose() { }
	}
}