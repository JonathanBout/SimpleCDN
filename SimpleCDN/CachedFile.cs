namespace SimpleCDN
{
	class CachedFile
	{
		public required byte[] Content { get; set; }
		public required MimeType MimeType { get; set; }
		public virtual DateTimeOffset LastModified { get; set; }
	}
}
