namespace SimpleCDN
{
	/// <summary>
	/// Represents the mime types SimpleCDN supports. All other mime types are considered <see cref="Unknown"/> and will be served as application/octet-stream.
	/// </summary>
	public enum MimeType : uint
	{
		// text
		/// <summary>
		/// HTML text mime type.
		/// </summary>
		HTML,
		/// <summary>
		/// Plain text mime type.
		/// </summary>
		Plain,
		/// <summary>
		/// CSS text mime type.
		/// </summary>
		CSS,
		/// <summary>
		/// Markdown text mime type.
		/// </summary>
		Markdown,
		// image
		/// <summary>
		/// PNG image mime type.
		/// </summary>
		PNG,
		/// <summary>
		/// JPEG image mime type.
		/// </summary>
		JPEG,
		/// <summary>
		/// GIF image mime type.
		/// </summary>
		GIF,
		/// <summary>
		/// SVG image mime type.
		/// </summary>
		SVG,
		/// <summary>
		/// WebP image mime type.
		/// </summary>
		WebP,
		/// <summary>
		/// ICO image mime type.
		/// </summary>
		ICO,
		// font
		/// <summary>
		/// Woff font mime type.
		/// </summary>
		Woff,
		/// <summary>
		/// Woff2 font mime type.
		/// </summary>
		Woff2,
		/// <summary>
		/// TTF font mime type.
		/// </summary>
		TTF,
		/// <summary>
		/// OTF font mime type.
		/// </summary>
		OTF,
		/// <summary>
		/// EOT font mime type.
		/// </summary>
		EOT,
		// other
		/// <summary>
		/// JSON mime type.
		/// </summary>
		JSON,
		/// <summary>
		/// Unknown mime type.
		/// </summary>
		Unknown
	}
}
