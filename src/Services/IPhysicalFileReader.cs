namespace SimpleCDN.Services
{
	public interface IPhysicalFileReader
	{
		bool CanLoadIntoArray(string path);
		bool CanLoadIntoArray(long size);
		bool DirectoryExists(string path);
		bool FileExists(string path);
		IEnumerable<string> GetFiles(string path, string pattern = "*");
		DateTimeOffset GetLastModified(string path);
		IEnumerable<FileSystemInfo> GetEntries(string path);
		byte[] LoadIntoArray(string path);
		Stream OpenFile(string path);
		bool IsDotFile(string path);
	}
}