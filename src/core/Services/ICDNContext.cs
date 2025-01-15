namespace SimpleCDN.Services
{
	internal interface ICDNContext
	{
		string BaseUrl { get; }

		string GetSystemFilePath(string filename);
	}
}
