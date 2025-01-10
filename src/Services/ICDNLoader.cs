using SimpleCDN.Helpers;

namespace SimpleCDN.Services
{
	public record CDNFile(byte[] Content, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression);

	public record BigCDNFile(string FilePath, string MediaType, DateTimeOffset LastModified, CompressionAlgorithm Compression) : CDNFile([], MediaType, LastModified, Compression);

	public record RedirectCDNFile(string Destination, bool Permanent) : CDNFile([], "text/html", DateTimeOffset.Now, CompressionAlgorithm.None);

	public interface ICDNLoader
	{
		CDNFile? GetFile(string path, params IEnumerable<CompressionAlgorithm> acceptedCompression);
	}
}
