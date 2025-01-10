using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Services.Implementations;
using SimpleCDN.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCDN.Tests.Unit
{
	public class PhysicalFileReaderTests
	{
		private string fileRoot = null!;
		private PhysicalFileReader reader = null!;
		private CDNConfiguration options = null!;

		[SetUp]
		public void SetUp()
		{
			fileRoot = Directory.CreateTempSubdirectory(typeof(PhysicalFileReaderTests).Name).FullName;
			options = new CDNConfiguration() { DataRoot = fileRoot };
			reader = new PhysicalFileReader(new MockLogger<PhysicalFileReader>(), new OptionsMock<CDNConfiguration>(options));
		}

		[Test]
		public void Test_DotFile_IsDetected()
		{
			string dotFilePath = Path.Combine(fileRoot, ".gitignore");
			string dotDirectoryPath = Path.Combine(fileRoot, ".directory");
			string dotDirectoryFilePath = Path.Combine(dotDirectoryPath, "file.txt");
			string nonDotFilePath = Path.Combine(fileRoot, "file.txt");
			string nonDotDirectoryPath = Path.Combine(fileRoot, "directory");
			string nonDotDirectoryFilePath = Path.Combine(nonDotDirectoryPath, "file.txt");

			File.CreateText(dotFilePath).Close();
			Directory.CreateDirectory(dotDirectoryPath);
			File.CreateText(dotDirectoryFilePath).Close();

			File.CreateText(nonDotFilePath).Close();
			Directory.CreateDirectory(nonDotDirectoryPath);
			File.CreateText(nonDotDirectoryFilePath).Close();

			Assert.Multiple(() =>
			{
				Assert.That(reader.IsDotFile(dotFilePath), Is.True);
				Assert.That(reader.IsDotFile(dotDirectoryPath), Is.True);
				Assert.That(reader.IsDotFile(dotDirectoryFilePath), Is.True);

				Assert.That(reader.IsDotFile(nonDotFilePath), Is.False);
				Assert.That(reader.IsDotFile(nonDotDirectoryPath), Is.False);
				Assert.That(reader.IsDotFile(nonDotDirectoryFilePath), Is.False);
			});
		}

		[Test]
		public void Test_File_LoadsIntoArray()
		{
			string filePath = Path.Combine(fileRoot, "file.txt");
			const string fileContent = "Hello, World!";

			File.WriteAllText(filePath, fileContent, Encoding.ASCII);

			var file = reader.LoadIntoArray(filePath);
			var actualContent = Encoding.ASCII.GetString(file);

			Assert.Multiple(() =>
			{
				Assert.That(file, Is.Not.Null);
				Assert.That(actualContent, Is.EqualTo(fileContent));
			});
		}

		[Test]
		public void Test_BigFile_DoesNotLoadIntoArray()
		{
			string filePath = Path.Combine(fileRoot, "file.txt");

			options.MaxCachedItemSize = 10;

			using (FileStream stream = File.Create(filePath))
			{
				stream.SetLength(options.MaxCachedItemSize * 1001); // 10 kB * 1001 = 10.01 MB
			}

			Assert.That(reader.CanLoadIntoArray(filePath), Is.False);
		}

		[Test]
		public void Test_SmallFile_LoadsIntoArray()
		{
			string filePath = Path.Combine(fileRoot, "file.txt");
			options.MaxCachedItemSize = 100;
			using (FileStream stream = File.Create(filePath))
			{
				stream.SetLength(options.MaxCachedItemSize - 10);
			}
			Assert.That(reader.CanLoadIntoArray(filePath), Is.True);
		}

		[Test]
		public void Test_File_Exists()
		{
			string filePath = Path.Combine(fileRoot, "file.txt");
			File.CreateText(filePath).Close();

			Assert.Multiple(() =>
			{
				Assert.That(reader.FileExists(filePath), Is.True);
				Assert.That(reader.FileExists(filePath + "x"), Is.False);
			});
		}

		[Test]
		public void Test_Directory_Exists()
		{
			string directoryPath = Path.Combine(fileRoot, "directory");
			Directory.CreateDirectory(directoryPath);
			Assert.Multiple(() =>
			{
				Assert.That(reader.DirectoryExists(directoryPath), Is.True);
				Assert.That(reader.DirectoryExists(directoryPath + "x"), Is.False);
			});
		}

		[Test]
		public void Test_GetFiles()
		{
			string directoryPath = Path.Combine(fileRoot, "directory");
			Directory.CreateDirectory(directoryPath);
			string filePath1 = Path.Combine(directoryPath, "file1.txt");
			string filePath2 = Path.Combine(directoryPath, "file2.txt");
			string filePath3 = Path.Combine(directoryPath, "file3.txt");
			File.CreateText(filePath1).Close();
			File.CreateText(filePath2).Close();
			File.CreateText(filePath3).Close();
			var files = reader.GetFiles(directoryPath).ToList();

			Assert.Multiple(() =>
			{
				Assert.That(files, Has.Count.EqualTo(3));
				Assert.That(files, Contains.Item(filePath1));
				Assert.That(files, Contains.Item(filePath2));
				Assert.That(files, Contains.Item(filePath3));
			});
		}

		[TearDown]
		public void TearDown()
		{
			// clean up over here so the tests don't have to worry about it
			Directory.Delete(fileRoot, true);
		}
	}
}
