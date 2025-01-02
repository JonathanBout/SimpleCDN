using SimpleCDN.Cache;
using SimpleCDN.Helpers;
using SimpleCDN.Services;
using SimpleCDN.Tests.Mocks;
using System.Text;

namespace SimpleCDN.Tests.Unit
{
	[TestFixture(TestName = "Cache Manager Tests")]
	public class CacheManagerTests
	{
		const string TEST_DATA_1 = "Hello, World!";
		const string TEST_DATA_2 = TEST_DATA_1 + TEST_DATA_1 + TEST_DATA_1 + TEST_DATA_1 + TEST_DATA_1 + TEST_DATA_1;
		const string TEST_DATA_3 = TEST_DATA_2 + TEST_DATA_2 + TEST_DATA_2 + TEST_DATA_2 + TEST_DATA_2 + TEST_DATA_2;
		const string TEST_PATH = "/hello.txt";

		[TestCase(TEST_DATA_1, TestName = "Cache Manager Add small data")]
		[TestCase(TEST_DATA_2, TestName = "Cache Manager Add medium data")]
		[TestCase(TEST_DATA_3, TestName = "Cache Manager Add big data")]
		public void Test_Add_Exists(string data)
		{
			var cacheImplementation = new DistributedCacheMock();

			var cache = new CacheManager(cacheImplementation);

			var file = new CachedFile
			{
				Content = Encoding.UTF8.GetBytes(data),
				Compression = CompressionAlgorithm.None,
				MimeType = MimeType.Text,
				LastModified = DateTimeOffset.Now,
				Size = 0 // can be anything as the content is not compressed
			};

			cache.CacheFile(TEST_PATH, file);

			Assert.That(cacheImplementation.Values, Has.Count.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(cacheImplementation.Values.Single().Key, Is.EqualTo(TEST_PATH));
				Assert.That(cacheImplementation.Values.Single().Value, Is.EqualTo(file.GetBytes()));
			});
		}

		[TestCase(TEST_DATA_1, TestName = "Cache Manager Add Remove small data")]
		[TestCase(TEST_DATA_2, TestName = "Cache Manager Add Remove medium data")]
		[TestCase(TEST_DATA_3, TestName = "Cache Manager Add Remove big data")]
		public void Test_Add_Remove_DoesNotExist(string data)
		{
			var cacheImplementation = new DistributedCacheMock();
			var cache = new CacheManager(cacheImplementation);

			var file = new CachedFile
			{
				Content = Encoding.UTF8.GetBytes(data),
				Compression = CompressionAlgorithm.None,
				MimeType = MimeType.Text,
				LastModified = DateTimeOffset.Now,
				Size = 0 // can be anything as the content is not compressed
			};

			cache.CacheFile(TEST_PATH, file);

			Assert.That(cacheImplementation.Values, Has.Count.EqualTo(1));

			cache.TryRemove(TEST_PATH);

			Assert.That(cacheImplementation.Values, Has.Count.EqualTo(0));
		}
	}
}
