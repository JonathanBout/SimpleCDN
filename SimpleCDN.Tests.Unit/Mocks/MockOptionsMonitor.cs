using Microsoft.Extensions.Options;

namespace SimpleCDN.Tests.Unit.Mocks
{
	internal class OptionsMock<T>(T value) : IOptionsMonitor<T>, IOptions<T>, IOptionsSnapshot<T> where T : class
	{
		public T CurrentValue => value;
		public T Value => CurrentValue;

		public T Get(string? name) => CurrentValue;
		public IDisposable? OnChange(Action<T, string?> listener) => new MockDisposable();
	}
}