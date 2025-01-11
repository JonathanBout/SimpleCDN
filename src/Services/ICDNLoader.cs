using SimpleCDN.Helpers;

namespace SimpleCDN.Services
{
	/// <summary>
	/// Represents a file that can be served by the CDN.
	/// Be aware that any method returning <see cref="CDNFile"/> may return a <see cref="BigCDNFile"/> or a <see cref="RedirectCDNFile"/>.
	/// </summary>
	public record CDNFile(byte[] Content, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression);

	/// <summary>
	/// Represents a file that is too large to be loaded into memory, and must be streamed from disk instead.
	/// </summary>
	public record BigCDNFile(string FilePath, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression) : CDNFile([], MediaType, LastModified, Compression);

	/// <summary>
	/// Represents a file that is a redirect to another location.
	/// </summary>
	/// <param name="Destination">The destination of the redirect.</param>
	/// <param name="Permanent">Whether the redirect is permanent or not</param>
	public record RedirectCDNFile(string Destination, bool Permanent) : CDNFile([], "text/html", DateTimeOffset.Now, CompressionAlgorithm.None);

	/// <summary>
	/// Represents a loader that can load files from the CDN.
	/// </summary>
	public interface ICDNLoader
	{
		/// <summary>
		/// Gets a file from the CDN.
		/// </summary>
		/// <param name="path">The path to the requested file, relative to the root of the CDN.</param>
		/// <param name="acceptedCompression">Allowed compression algorithms.</param>
		/// <returns>
		/// The file if it exists, or <see langword="null"/> if not found.
		/// If the file is too big to load into memory, returns <see cref="BigCDNFile"/>.
		/// If the file is available but at another location, returns <see cref="RedirectCDNFile"/>.
		/// The redirect path is relative to the root of the CDN.
		/// </returns>
		CDNFile? GetFile(string path, params IEnumerable<CompressionAlgorithm> acceptedCompression);
	}
}
