using SimpleCDN.Configuration;
using SimpleCDN.Services;
using SimpleCDN.Services.Implementations;
using SimpleCDN.Tests.Mocks;
using System.Text;

namespace SimpleCDN.Tests.Unit
{
	[TestFixture]
	public class CDNLoaderTests
	{
		const string JSON_FILENAME = "file.json";
		const string TEXT_FILENAME = "data/file.txt";
		const string SVG_FILENAME = "data/image.svg";
		const string DEEPLY_NESED_FILENAME = "data/nested/deeply/nested/file.txt";
		const string INACCESSIBLE_FILENAME = "../inaccesible.txt";
		const string DOTFILE_FILENAME = ".gitignore";

		const string DOTDIRECTORY_WITH_FILE_FILENAME = ".directory/file.txt";

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
			["/" + DOTFILE_FILENAME] = new(DateTime.Now, Encoding.UTF8.GetBytes("[Bb]in/")),
			["/" + DOTDIRECTORY_WITH_FILE_FILENAME] = new(DateTime.Now, Encoding.UTF8.GetBytes("Hello, world!")),
		};

		private static CDNLoader CreateLoader(Action<CDNConfiguration>? configure = null)
		{
			var options = new OptionsMock<CDNConfiguration>(new() { DataRoot = "/" });

			if (configure is not null)
			{
				configure(options.CurrentValue);
			}

			return new CDNLoader(
				new MockWebHostEnvironment(),
				options,
				new IndexGenerator(options, new MockLogger<IndexGenerator>()),
				new MockCacheManager(),
				new MockLogger<CDNLoader>(),
				new MockPhysicalFileReader(Files));
		}

		[TestCase("../inaccesible.txt")]
		[TestCase("/../inaccesible.txt")]
		[TestCase("/../../inaccesible.txt")]
		[TestCase("../../inaccesible.txt")]
		[TestCase("/data/../../inaccesible.txt")]
		public void Test_ParentDirectory_IsInaccesible(string path)
		{
			CDNLoader loader = CreateLoader();

			Assert.That(loader.GetFile(path), Is.Null);
		}

		[TestCase("/nx.txt")]
		[TestCase("nx.txt")]
		[TestCase("/data/nx.txt")]
		[TestCase("/data/nx/nx.txt")]
		public void Test_NonExistentFile_IsInaccesible(string name)
		{
			CDNLoader loader = CreateLoader();
			Assert.That(loader.GetFile(name), Is.Null);
		}

		[TestCase(JSON_FILENAME, JSON_CONTENT, "application/json")]
		[TestCase(TEXT_FILENAME, TEXT_CONTENT, "text/plain")]
		[TestCase(SVG_FILENAME, SVG_CONTENT, "image/svg+xml")]
		[TestCase("/" + JSON_FILENAME, JSON_CONTENT, "application/json")]
		[TestCase("/" + TEXT_FILENAME, TEXT_CONTENT, "text/plain")]
		[TestCase("/" + SVG_FILENAME, SVG_CONTENT, "image/svg+xml")]
		[TestCase("/../" + SVG_FILENAME, SVG_CONTENT, "image/svg+xml")]
		[TestCase("/non-existent/../" + SVG_FILENAME, SVG_CONTENT, "image/svg+xml")]
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
