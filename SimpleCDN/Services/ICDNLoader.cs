namespace SimpleCDN.Services
{
	public interface ICDNLoader
	{
		CDNFile? GetFile(string path);
	}
}