using System.Numerics;

namespace SimpleCDN
{
	public static class Extensions
	{
		public static WebApplication MapCDNEndpoints(this WebApplication builder)
		{
			builder.MapGet("/{*route}", (CDNLoader loader, string route = "") =>
			{
				if (loader.GetFile(route) is CDNFile file)
				{
					return Results.File(file.Content, file.MediaType, lastModified: file.LastModified);
				}

				return Results.NotFound();
			}).CacheOutput(policy =>
			{
				policy.Cache()
					.Expire(TimeSpan.FromMinutes(1));
			});

			return builder;
		}

		static readonly string[] sizeNames = ["", "k", "M", "G", "T"];

		public static string FormatByteCount(this long number)
		{
			bool isNegative = false;
			if (number < 0)
			{
				isNegative = true;
				number = -number;
			}

			int sizeNameIndex = 0;

			double result = number;

			for (; sizeNameIndex < sizeNames.Length - 1; sizeNameIndex++)
			{
				var div = result / 1000;
				if (div < 1)
				{
					break;
				}

				result = div;
			}

			if (isNegative)
			{
				result = -result;
			}

			return $"{result:0.##}{sizeNames[sizeNameIndex]}B";
		}
	}
}
