﻿using Microsoft.Extensions.Caching.Distributed;
using SimpleCDN.Configuration;
using SimpleCDN.Services.Caching.Implementations;
using SimpleCDN.Tests.Mocks;
using System.Text;

namespace SimpleCDN.Tests.Unit
{
	public class SizeLimitedCacheTests
	{
		private static InMemoryCache CreateCache(Action<InMemoryCacheConfiguration>? configure = null, Action<CacheConfiguration>? configureCache = null)
		{
			var options = new InMemoryCacheConfiguration();
			var cacheOptions = new CacheConfiguration();

			configure?.Invoke(options);
			configureCache?.Invoke(cacheOptions);

			var optionsMock = new OptionsMock<InMemoryCacheConfiguration>(options);
			var cacheOptionsMock = new OptionsMock<CacheConfiguration>(cacheOptions);

			return new InMemoryCache(optionsMock, cacheOptionsMock, new MockLogger<InMemoryCache>());
		}

		[Test]
		public void Test_UnaddedFile_Misses()
		{
			InMemoryCache cache = CreateCache();

			Assert.Multiple(() =>
			{
				Assert.That(cache.Count, Is.Zero);
				Assert.That(cache.Size, Is.Zero);
				Assert.That(cache.Keys, Is.Empty);
			});
			Assert.That(cache.Get("/hello.txt"), Is.Null);
		}

		[Test]
		public void Test_AddedFile_Hits()
		{
			InMemoryCache cache = CreateCache();
			const string TEST_DATA = "Hello, World!";
			const string TEST_PATH = "/hello.txt";
			cache.Set(TEST_PATH, Encoding.UTF8.GetBytes(TEST_DATA), new DistributedCacheEntryOptions());
			Assert.Multiple(() =>
			{
				Assert.That(cache.Count, Is.EqualTo(1));
				Assert.That(cache.Size, Is.EqualTo(TEST_DATA.Length));
				Assert.That(cache.Keys, Is.EquivalentTo([TEST_PATH]));
			});
			Assert.That(cache.Get(TEST_PATH), Is.EqualTo(Encoding.UTF8.GetBytes(TEST_DATA)));
		}

		[TestCase(0, true)]
		[TestCase(1, true)]
		[TestCase(1000, true)]
		[TestCase(1001, false)]
		[TestCase(10000, false)]
		public void Test_AddedFile_TooLarge(int size, bool shouldPass)
		{
			InMemoryCache cache = CreateCache(options => options.MaxSize = 1);
			const string TEST_PATH = "/hello.txt";
			string testData = new string('*', size);
			cache.Set(TEST_PATH, Encoding.UTF8.GetBytes(testData), new DistributedCacheEntryOptions());

			if (shouldPass)
			{
				Assert.Multiple(() =>
				{
					Assert.That(cache.Count, Is.EqualTo(1));
					Assert.That(cache.Size, Is.EqualTo(testData.Length));
					Assert.That(cache.Keys, Is.EquivalentTo([TEST_PATH]));
				});
				Assert.That(cache.Get(TEST_PATH), Is.Not.Null);
			} else
			{
				Assert.Multiple(() =>
				{
					Assert.That(cache.Count, Is.Zero);
					Assert.That(cache.Size, Is.Zero);
					Assert.That(cache.Keys, Is.Empty);
				});
				Assert.That(cache.Get(TEST_PATH), Is.Null);
			}
		}
	}
}
