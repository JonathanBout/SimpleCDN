using SimpleCDN.Configuration;
using SimpleCDN.Services;
using SimpleCDN.Services.Implementations;
using SimpleCDN.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using static SimpleCDN.Services.Implementations.IndexGenerator;
using static System.Net.WebRequestMethods;

namespace SimpleCDN.Tests.Unit
{
	public class IndexGeneratorTests
	{
		private static IndexGenerator CreateIndexGenerator(bool allowDotfileAcces)
		{
			return new IndexGenerator(
				new OptionsMock<CDNConfiguration>(
					new() { GenerateIndexJson = true, AllowDotFileAccess = allowDotfileAcces, ShowDotFiles = true }),
				new MockLogger<IndexGenerator>(),
				new MockCDNContext());
		}

		private string TempDirectory;

		readonly string[] files = ["a.txt", "b.txt", ".c", "d.csv"];
		readonly string[] dirs = ["dir_a", "DIRB", ".dir-c", "dirD"];

		[OneTimeSetUp]
		public void Setup()
		{
			TempDirectory = Path.Combine(Path.GetTempPath(), "simplecdn-test-" + Guid.NewGuid().ToString());
			Directory.CreateDirectory(TempDirectory);

			foreach (var file in files)
			{
				System.IO.File.Create(Path.Join(TempDirectory, file)).Dispose();
			}
			foreach (var dir in dirs)
			{
				Directory.CreateDirectory(Path.Join(TempDirectory, dir));
			}
		}

		[TestCase("/index.json", true, false)]
		[TestCase("/nx/index.json", false, false)]
		[TestCase("/index.json", true, true)]
		[TestCase("/nx/index.json", false, true)]
		public void Test_GeneratedIndexJson(string route, bool exists, bool includeDotFiles)
		{
			IndexGenerator generator = CreateIndexGenerator(includeDotFiles);
			byte[]? content = generator.GenerateIndexJson(Path.GetDirectoryName(Path.Join(TempDirectory, route))!, route);

			Assert.That(content is not null, Is.EqualTo(exists));
			if (content is not null)
			{
				JsonIndexModel? body = JsonSerializer.Deserialize(content, SourceGenerationContext.Default.JsonIndexModel);
				Assert.That(body, Is.Not.Null);
				int count = files.Length + dirs.Length;
				int hiddenFiles = 0;
				if (!includeDotFiles)
				{
					hiddenFiles += files.Count(f => f.StartsWith('.')) + dirs.Count(d => d.StartsWith('.'));
				}
				count -= hiddenFiles;
				Assert.That(body.Items, Has.Count.EqualTo(count));
				foreach (JsonIndexItemModel item in body.Items)
				{
					Assert.That(item.Name, Is.AnyOf(files).Or.AnyOf(dirs));
				}
			}
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			Directory.Delete(TempDirectory, true);
		}
	}
}
