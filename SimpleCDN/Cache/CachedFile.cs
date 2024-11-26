namespace SimpleCDN.Cache
{
	class CachedFile
	{
		public bool IsCompressed { get; set; } = false;
		public required byte[] Content { get; set; }
		public required MimeType MimeType { get; set; }
		public virtual DateTimeOffset LastModified { get; set; }
	}
}
