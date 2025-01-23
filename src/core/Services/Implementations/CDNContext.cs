namespace SimpleCDN.Services.Implementations
{
	internal class CDNContext : ICDNContext
	{
		public CDNContext(IHttpContextAccessor ctxAccessor)
		{
			if (ctxAccessor.HttpContext is not HttpContext ctx)
			{
				BaseUrl = "/";
				return;
			}

			var requestUrl = ctx.Request.RouteValues[GlobalConstants.CDNRouteValueKey] as string;

			requestUrl ??= "/";

			if (ctx.Request.Path.Value is string fullPath)
			{
				if (fullPath.EndsWith(requestUrl))
				{
					BaseUrl = fullPath[..^requestUrl.Length];
				} else
				{
					BaseUrl = fullPath;
				}
			} else
			{
				BaseUrl = "/";
			}

			if (!BaseUrl.EndsWith('/'))
			{
				BaseUrl += '/';
			}

			if (!BaseUrl.StartsWith('/'))
			{
				BaseUrl = '/' + BaseUrl;
			}
		}

		public string BaseUrl { get; }

		public string GetSystemFilePath(string filename)
		{
			if (filename.StartsWith('/'))
			{
				filename = filename[1..];
			}
			return $"{BaseUrl}{GlobalConstants.SystemFilesRelativePath}/{filename}";
		}
	}
}
