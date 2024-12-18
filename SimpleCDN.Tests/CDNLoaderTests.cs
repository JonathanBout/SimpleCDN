using SimpleCDN.Configuration;
using SimpleCDN.Services;
using SimpleCDN.Tests.Mocks;
using System.Text;

namespace SimpleCDN.Tests
{
	[TestFixture(TestName = "CDN Loader Tests")]
	public class CDNLoaderTests
	{
		const string JSON_FILENAME = "file.json";
		const string TEXT_FILENAME = "data/file.txt";
		const string SVG_FILENAME = "data/image.svg";
		const string DEEPLY_NESED_FILENAME = "data/nested/deeply/nested/file.txt";
		const string INACCESSIBLE_FILENAME = "../inaccesible.txt";

		const string JSON_CONTENT = "{}";
		const string TEXT_CONTENT = "Hello, world!";
		const string SVG_CONTENT = "<svg></svg>";

		private static readonly Dictionary<string, MockFile> Files = new()
		{
			["/" + JSON_FILENAME] = new(DateTime.Now, Encoding.UTF8.GetBytes(JSON_CONTENT)),
			["/" + TEXT_FILENAME] = new(DateTime.Now, Encoding.UTF8.GetBytes(TEXT_CONTENT)),
			["/" + SVG_FILENAME] = new(DateTime.Now, Encoding.UTF8.GetBytes(SVG_CONTENT)),
			["/" + DEEPLY_NESED_FILENAME] = new(DateTime.Now, Encoding.UTF8.GetBytes(TEXT_CONTENT)),
			["/" + INACCESSIBLE_FILENAME] = new(DateTime.Now, Encoding.UTF8.GetBytes(":(")),
		};

		private static CDNLoader CreateLoader()
		{
			var options = new OptionsMock<CDNConfiguration>(new() { DataRoot = "/" });

			return new CDNLoader(
				new MockWebHostEnvironment(),
				options,
				new IndexGenerator(options, new MockLogger<IndexGenerator>()),
				new MockCacheManager(),
				new MockLogger<CDNLoader>(),
				new MockPhysicalFileReader(Files));
		}

		[TestCase("../inaccesible.txt", TestName = "File in parent directory")]
		[TestCase("/../inaccesible.txt", TestName = "File in root's parent directory")]
		[TestCase("/../../inaccesible.txt", TestName = "File in root parent's parent directory")]
		[TestCase("../../inaccesible.txt", TestName = "File in parent's parent directory")]
		[TestCase("/data/../../inaccesible.txt", TestName = "File in /data's parent's parent directory")]
		public void Test_ParentDirectory_IsInaccesible(string path)
		{
			CDNLoader loader = CreateLoader();

			Assert.That(loader.GetFile(path), Is.Null);
		}

		[TestCase("/nx.txt", TestName = "Non-existent file in root")]
		[TestCase("nx.txt", TestName = "Non-existent file in current directory")]
		[TestCase("/data/nx.txt", TestName = "Non-existent file in /data directory")]
		[TestCase("/data/nx/nx.txt", TestName = "File in Non-existent directory")]
		public void Test_NonExistentFile_IsInaccesible(string name)
		{
			CDNLoader loader = CreateLoader();
			Assert.That(loader.GetFile(name), Is.Null);
		}

		[TestCase(JSON_FILENAME, JSON_CONTENT, "application/json", TestName = "Existing JSON File")]
		[TestCase(TEXT_FILENAME, TEXT_CONTENT, "text/plain", TestName = "Existing Plain Text File")]
		[TestCase(SVG_FILENAME, SVG_CONTENT, "image/svg+xml", TestName = "Existing SVG File")]
		[TestCase("/" + JSON_FILENAME, JSON_CONTENT, "application/json", TestName = "Existing JSON File relative to root")]
		[TestCase("/" + TEXT_FILENAME, TEXT_CONTENT, "text/plain", TestName = "Existing Plain Text File relative to root")]
		[TestCase("/" + SVG_FILENAME, SVG_CONTENT, "image/svg+xml", TestName = "Existing SVG File relative to root")]
		[TestCase("/../" + SVG_FILENAME, SVG_CONTENT, "image/svg+xml", TestName = "Existing SVG File with path traversal at root")]
		[TestCase("/non-existent/../" + SVG_FILENAME, SVG_CONTENT, "image/svg+xml", TestName = "Existing SVG File with path traversal")]
		public void Test_AccessibleFiles(string name, string content, string mediaType)
		{
			CDNLoader loader = CreateLoader();
			CDNFile? file = loader.GetFile(name);
			Assert.That(file, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(file!.Content, Is.Not.Null);
				Assert.That(Encoding.UTF8.GetString(file.Content), Is.EqualTo(content));
				Assert.That(file.MediaType, Is.EqualTo(mediaType));
			});
		}
	}
}
