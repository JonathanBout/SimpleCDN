namespace SimpleCDN.Cache
{
	class CachedFile
	{
		private long _size;

		public CachedFile() { }

		public bool IsCompressed { get; set; } = false;
		public required long Size
		{
			get
			{
				if (!IsCompressed || _size <= 0)
				{
					return Content.Length;
				}

				return _size;
			}
			set
			{
				_size = value;
			}
		}

		public required byte[] Content { get; set; }
		public required MimeType MimeType { get; set; }
		public virtual DateTimeOffset LastModified { get; set; }
	}
}
