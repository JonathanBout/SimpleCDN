using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using System.Text;

namespace SimpleCDN.Tests.Unit
{
	[TestFixture(TestName = "Cached File Binarization Tests")]
	public class CachedFileBinarizationTests
	{
		[TestCase(TestName = "Convert To-From")]
		public void ConvertToFrom()
		{
			var file = new CachedFile
			{
				Content = Encoding.UTF8.GetBytes("Hello, World!"),
				Compression = CompressionAlgorithm.None,
				MimeType = MimeType.Text,
				LastModified = DateTimeOffset.Now,
				Size = 0 // can be anything as the content is not compressed
			};

			var bytes = file.GetBytes();
			var newFile = CachedFile.FromBytes(bytes);

			Assert.That(newFile, Is.Not.Null);

			Assert.Multiple(() =>
			{
				Assert.That(newFile!.Content, Is.EqualTo(file.Content));
				Assert.That(newFile.Compression, Is.EqualTo(file.Compression));
				Assert.That(newFile.MimeType, Is.EqualTo(file.MimeType));
				Assert.That(newFile.LastModified, Is.EqualTo(file.LastModified));
				Assert.That(newFile.Size, Is.EqualTo(file.Size));
			});
		}
	}
}
