using SimpleCDN.Helpers;

namespace SimpleCDN.Services
{
	internal interface ISystemFileReader
	{
		public CDNFile? GetSystemFile(ReadOnlySpan<char> path, IEnumerable<CompressionAlgorithm> acceptedCompression);
	}
}
