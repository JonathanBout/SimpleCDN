using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Tests
{
	[TestFixture(TestName = "CDN Loader Tests")]
	public class CDNLoaderTests
	{
		const string JSON_FILENAME = "file.json";
		const string TEXT_FILENAME = "data/file.txt";
		const string SVG_FILENAME = "data/image.svg";

		const string JSON_CONTENT = "{}";
		const string TEXT_CONTENT = "Hello, world!";
		const string SVG_CONTENT = "<svg></svg>";


		private static string BaseFolder => Path.Combine(Path.GetTempPath(), "SimpleCDN.Tests");
		private static string TempFolder => Path.Combine(BaseFolder, "wwwroot");
		private static string InaccesibleFile => Path.Combine(BaseFolder, "inaccesible.txt");
		private static string JSONFile => Path.Combine(TempFolder, JSON_FILENAME);
		private static string TextFile => Path.Combine(TempFolder, TEXT_FILENAME);
		private static string SVGFile => Path.Combine(TempFolder, SVG_FILENAME);

		private static CDNLoader CreateLoader()
		{
			var options = new OptionsMock<CDNConfiguration>(new() { DataRoot = TempFolder });

			return new CDNLoader(new MockWebHostEnvironment(), options, new IndexGenerator(options));
		}

		[SetUp]
		public void Setup()
		{
			// Clean up previous test data if it exists
			// This could happen if the previous test was interrupted
			if (Directory.Exists(BaseFolder))
			{
				Directory.Delete(BaseFolder, true);
			}

			Directory.CreateDirectory(TempFolder);
			File.WriteAllText(Path.Combine(BaseFolder, InaccesibleFile), ":(");
			File.WriteAllText(JSONFile, JSON_CONTENT);
			Directory.CreateDirectory(Path.Combine(TempFolder, "data"));
			File.WriteAllText(TextFile, TEXT_CONTENT);
			File.WriteAllText(SVGFile, SVG_CONTENT);
		}

		[TestCase("../inaccesible.txt", TestName = "File in parent directory")]
		[TestCase("/../inaccesible.txt", TestName = "File in root's parent directory")]
		[TestCase("/../../inaccesible.txt", TestName = "File in root parent's parent directory")]
		[TestCase("../../inaccesible.txt", TestName = "File in parent's parent directory")]
		[TestCase("/data/../../inaccesible.txt", TestName = "File in /data's parent's parent directory")]
		public void Test_ParentDirectory_IsInaccesible(string path)
		{
			var loader = CreateLoader();

			Assert.That(loader.GetFile(path), Is.Null);
		}

		[TestCase("/nx.txt", TestName = "Non-existent file in root")]
		[TestCase("nx.txt", TestName = "Non-existent file in current directory")]
		[TestCase("/data/nx.txt", TestName = "Non-existent file in /data directory")]
		[TestCase("/data/nx/nx.txt", TestName = "File in Non-existent directory")]
		public void Test_NonExistentFile_IsInaccesible(string name)
		{
			var loader = CreateLoader();
			Assert.That(loader.GetFile(name), Is.Null);
		}

		[TestCase(JSON_FILENAME, JSON_CONTENT, "application/json", TestName = "Existing JSON File")]
		[TestCase(TEXT_FILENAME, TEXT_CONTENT, "text/plain", TestName = "Existing Plain Text File")]
		[TestCase(SVG_FILENAME, SVG_CONTENT, "image/svg+xml", TestName = "Existing SVG File")]
		[TestCase("/" + JSON_FILENAME, JSON_CONTENT, "application/json", TestName = "Existing JSON File relative to root")]
		[TestCase("/" + TEXT_FILENAME, TEXT_CONTENT, "text/plain", TestName = "Existing Plain Text File relative to root")]
		[TestCase("/" + SVG_FILENAME, SVG_CONTENT, "image/svg+xml", TestName = "Existing SVG File relative to root")]
		public void Test_AccessibleFiles(string name, string content, string mediaType)
		{
			var loader = CreateLoader();
			var file = loader.GetFile(name);
			Assert.That(file, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(file!.Content, Is.Not.Null);
				Assert.That(Encoding.UTF8.GetString(file.Content), Is.EqualTo(content));
				Assert.That(file.MediaType, Is.EqualTo(mediaType));
			});
		}

		[TearDown]
		public void TearDown()
		{
			Directory.Delete(BaseFolder, true);
		}
	}
}
