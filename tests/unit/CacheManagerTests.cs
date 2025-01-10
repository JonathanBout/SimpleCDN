using SimpleCDN.Cache;
using SimpleCDN.Configuration;
using SimpleCDN.Helpers;
using SimpleCDN.Services.Caching.Implementations;
using SimpleCDN.Tests.Mocks;
using System.Text;

namespace SimpleCDN.Tests.Unit
{
	[TestFixture]
	public class CacheManagerTests
	{
		const string TEST_DATA_1 = "Hello, World!";
		const string TEST_DATA_2 = TEST_DATA_1 + TEST_DATA_1 + TEST_DATA_1 + TEST_DATA_1 + TEST_DATA_1 + TEST_DATA_1;
		const string TEST_DATA_3 = TEST_DATA_2 + TEST_DATA_2 + TEST_DATA_2 + TEST_DATA_2 + TEST_DATA_2 + TEST_DATA_2;
		const string TEST_PATH = "/hello.txt";

		private static (CacheManager manager, DistributedCacheMock implementation) CreateCache(Action<CacheConfiguration>? configure = null)
		{
			var cacheImplementation = new DistributedCacheMock();

			var options = new CacheConfiguration();
			configure?.Invoke(options);

			var cache = new CacheManager(cacheImplementation, new OptionsMock<CacheConfiguration>(options), new MockLogger<CacheManager>());
			return (cache, cacheImplementation);
		}

		[TestCase(TEST_DATA_1)]
		[TestCase(TEST_DATA_2)]
		[TestCase(TEST_DATA_3)]
		public void Test_Add_Exists(string data)
		{
			(CacheManager cache, DistributedCacheMock cacheImplementation) = CreateCache();

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

		[TestCase(TEST_DATA_1)]
		[TestCase(TEST_DATA_2)]
		[TestCase(TEST_DATA_3)]
		public void Test_Add_Remove_DoesNotExist(string data)
		{
			(CacheManager cache, DistributedCacheMock cacheImplementation) = CreateCache();

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
