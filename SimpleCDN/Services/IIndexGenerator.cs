namespace SimpleCDN.Services
{
	public interface IIndexGenerator
	{
		byte[]? GenerateIndex(string absolutePath, string rootRelativePath);
	}
}