using BenchmarkDotNet.Attributes;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using SimpleCDN.Services;
using SimpleCDN.Tests.Mocks;
using SimpleCDN.Services.Implementations;

namespace SimpleCDN.Benchmarks
{
	[MemoryDiagnoser]
	public class PhysicalFileReaderBenchmarks
	{
		private PhysicalFileReader reader = null!;
		private string dataRoot = null!;

		const string DOTFILE = ".file";
		const string FILE = "file";

		const string DOT_DIRECTORY = ".directory";
		const string DIRECTORY = "directory";

		const string DOT_DIRECTORY_FILE = ".directory/file.txt";
		const string DIRECTORY_FILE = "directory/file.txt";

		public static string[] Paths { get; } = [DOTFILE, FILE, DOT_DIRECTORY, DIRECTORY, DOT_DIRECTORY_FILE, DIRECTORY_FILE];

		[GlobalSetup]
		public void Setup()
		{
			dataRoot = Directory.CreateTempSubdirectory("SimpleCDN-Benchmarks-").FullName;

			reader = new PhysicalFileReader(new MockLogger<PhysicalFileReader>(), new OptionsMock<CDNConfiguration>(new CDNConfiguration { DataRoot = dataRoot }));

			Directory.CreateDirectory(Path.Combine(dataRoot, DOT_DIRECTORY));
			Directory.CreateDirectory(Path.Combine(dataRoot, DIRECTORY));

			File.OpenWrite(Path.Combine(dataRoot, DOTFILE)).Close();
			File.OpenWrite(Path.Combine(dataRoot, FILE)).Close();

			File.OpenWrite(Path.Combine(dataRoot, DOT_DIRECTORY_FILE)).Close();
			File.OpenWrite(Path.Combine(dataRoot, DIRECTORY_FILE)).Close();
		}

		[Benchmark]
		[ArgumentsSource(nameof(Paths))]
		public bool IsDotfile(string path)
		{
			return reader.IsDotFile(Path.Combine(dataRoot, path));
		}

		[Benchmark]
		[ArgumentsSource(nameof(Paths))]
		public bool IsDotfileStrings(string path)
		{
			// This method has the same logic as the one in the PhysicalFileReader class, but it's using strings instead of spans.
			// This is to compare the performance of the two methods.
			path = Path.Combine(dataRoot, path);

			if (!File.Exists(path) && !Directory.Exists(path))
			{
				return false;
			}

			string rootRelativePath = path.Replace(dataRoot, string.Empty);

			foreach (string section in rootRelativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{
				if (section.StartsWith('.'))
				{
					return true;
				}
			}

			return false;
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			Directory.Delete(dataRoot, true);
		}
	}
}
