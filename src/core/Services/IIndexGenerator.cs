using System.Text;

namespace SimpleCDN.Services
{
	/// <summary>
	/// Represents a service that generates an index file for a directory.
	/// </summary>
	internal interface IIndexGenerator
	{
		/// <summary>
		/// Generates an index file for the directory at the given path.
		/// </summary>
		/// <param name="absolutePath">The absolute path to the directory.</param>
		/// <param name="rootRelativePath">The CDN root-relative path to the directory.</param>
		/// <returns>
		/// The generated index file encoded with <see cref="Encoding.UTF8"/>,
		/// or <see langword="null"/> if the directory does not exist or is inaccesible.
		/// </returns>
		byte[]? GenerateIndex(string absolutePath, string rootRelativePath);
	}
}
