using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SimpleCDN.Standalone
{
	public class CustomHealthReport
	{
		public HealthStatus Status { get; init; }
		public TimeSpan TotalDuration { get; init; }
		public IReadOnlyDictionary<string, CustomHealthReportEntry> Entries { get; init; } = new Dictionary<string, CustomHealthReportEntry>();

		public static CustomHealthReport FromHealthReport(HealthReport report)
		{
			return new CustomHealthReport
			{
				Entries = report.Entries.ToDictionary(
					kvp => kvp.Key,
					kvp => CustomHealthReportEntry.FromHealthReportEntry(kvp.Value)
				),
				Status = report.Status,
				TotalDuration = report.TotalDuration
			};
		}

		public readonly struct CustomHealthReportEntry
		{
			public readonly HealthStatus Status { get; init; }
			public readonly string? Description { get; init; }
			public readonly TimeSpan Duration { get; init; }
			public readonly IReadOnlyDictionary<string, object> Data { get; init; }
			public readonly IEnumerable<string> Tags { get; init; }
			public readonly bool HasException { get; init; }

			public static CustomHealthReportEntry FromHealthReportEntry(HealthReportEntry entry)
			{
				return new CustomHealthReportEntry
				{
					Status = entry.Status,
					Description = entry.Description,
					Duration = entry.Duration,
					Data = entry.Data,
					Tags = entry.Tags,
					HasException = entry.Exception != null
				};
			}
		}
	}
}
