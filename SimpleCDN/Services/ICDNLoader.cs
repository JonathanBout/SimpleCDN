using SimpleCDN.Helpers;

namespace SimpleCDN.Services
{
	public interface ICDNLoader
	{
		CDNFile? GetFile(string path, params IEnumerable<CompressionAlgorithm> acceptedCompression);
	}
}
