namespace SimpleCDN.Cache
{
	class CachedIndexFile : CachedFile
	{
		public required string DirectoryName { get; set; }
		public override DateTimeOffset LastModified
		{
			get => Directory.GetLastWriteTimeUtc(DirectoryName);
			set { }
		}
	}
}
