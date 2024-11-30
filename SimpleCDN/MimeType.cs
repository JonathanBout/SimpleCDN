namespace SimpleCDN
{
	/// <summary>
	/// Represents the mime types SimpleCDN supports. All other mime types are considered <see cref="Unknown"/> and will be served as application/octet-stream.
	/// </summary>
	public enum MimeType : uint
	{
		// text
		HTML, Text, CSS,
		// image
		PNG, JPEG, GIF, SVG, WebP, ICO,
		// font
		Woff, Woff2, TTF, OTF, EOT,
		// other
		JSON, Unknown
	}
}
