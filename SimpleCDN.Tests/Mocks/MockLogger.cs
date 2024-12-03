using Microsoft.Extensions.Logging;

namespace SimpleCDN.Tests.Mocks
{
	internal class MockLogger<T> : ILogger<T>
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => new MockDisposable();
		public bool IsEnabled(LogLevel logLevel) => true;
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
	}
}
