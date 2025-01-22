namespace SimpleCDN.Services
{
	/// <summary>
	/// Interface for reading physical files.
	/// </summary>
	internal interface IPhysicalFileReader
	{
		/// <summary>
		/// Checks if the directory at the specified path exists.
		/// </summary>
		/// <param name="path">The path of the directory.</param>
		/// <returns>True if the directory exists, otherwise false.</returns>
		bool DirectoryExists(string path);

		/// <summary>
		/// Checks if the file at the specified path exists.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		/// <returns>True if the file exists, otherwise false.</returns>
		bool FileExists(string path);

		/// <summary>
		/// Gets the files in the specified directory that match the given pattern.
		/// </summary>
		/// <param name="path">The path of the directory.</param>
		/// <param name="pattern">The search pattern. Default is "*".</param>
		/// <returns>An enumerable collection of file names.</returns>
		IEnumerable<string> GetFiles(string path, string pattern = "*");

		/// <summary>
		/// Gets the last modified date and time of the file at the specified path.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		/// <returns>The last modified date and time.</returns>
		DateTimeOffset GetLastModified(string path);

		/// <summary>
		/// Gets the file system entries in the specified directory.
		/// </summary>
		/// <param name="path">The path of the directory.</param>
		/// <returns>An enumerable collection of file system entries.</returns>
		IEnumerable<FileSystemInfo> GetEntries(string path);

		/// <summary>
		/// Loads the file at the specified path into a byte array.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		/// <returns>A byte array containing the file's contents.</returns>
		byte[] LoadIntoArray(string path);

		/// <summary>
		/// Opens the file at the specified path as a stream.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		/// <returns>A stream for reading the file.</returns>
		Stream OpenFile(string path);

		/// <summary>
		/// Determines if the file at the specified path is a dot file.
		/// </summary>
		/// <param name="path">The path of the file.</param>
		/// <returns>True if the file is a dot file, otherwise false.</returns>
		bool IsDotFile(string path);

		/// <summary>
		/// Gets the size of the file at the specified path.
		/// </summary>
		/// <param name="path">The path to the file</param>
		/// <returns>The size of the file, or <see cref="long.MaxValue"/> if any error occurs.</returns>
		long GetSize(string path);
	}
}
