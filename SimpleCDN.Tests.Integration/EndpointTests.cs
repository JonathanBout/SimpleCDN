﻿using Microsoft.AspNetCore.Mvc.Testing;

namespace SimpleCDN.Tests.Integration
{
	public class EndpointTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly CustomWebApplicationFactory _webApplicationFactory;

		const string TEXT_FILE_NAME = "test.txt";
		const string TEXT_FILE_CONTENT = "Hello, World!";

		const string JSON_FILE_NAME = "data/test.json";
		const string JSON_FILE_CONTENT = """{"key": "value"}""";

		public EndpointTests(CustomWebApplicationFactory webApplicationFactory)
		{
			_webApplicationFactory = webApplicationFactory;

			var dataRoot = _webApplicationFactory.DataRoot;

			Directory.CreateDirectory(Path.Combine(dataRoot, "data"));

			// create some files
			File.WriteAllText(Path.Combine(_webApplicationFactory.DataRoot, TEXT_FILE_NAME), TEXT_FILE_CONTENT);
			File.WriteAllText(Path.Combine(_webApplicationFactory.DataRoot, JSON_FILE_NAME), JSON_FILE_CONTENT);
		}

		[Fact]
		public async Task Test_Accesible()
		{
			var client = _webApplicationFactory.CreateClient();

			var response = await client.GetAsync("/");

			Assert.True(response.IsSuccessStatusCode, response.StatusCode.ToString());
		}

		[Fact]
		public async Task Test_AutoGeneratedIndex()
		{
			var client = _webApplicationFactory.CreateClient();
			var response = await client.GetAsync("/");
			var content = await response.Content.ReadAsStringAsync();
			Assert.Contains(CustomWebApplicationFactory.GENERATED_INDEX_ID, content);
		}

		[Theory]
		[InlineData("/" + TEXT_FILE_NAME, TEXT_FILE_CONTENT)]
		[InlineData("/../" + TEXT_FILE_NAME, TEXT_FILE_CONTENT)]
		[InlineData("/data/../" + TEXT_FILE_NAME, TEXT_FILE_CONTENT)]
		[InlineData("/" + JSON_FILE_NAME, JSON_FILE_CONTENT)]
		[InlineData("/data/../" + JSON_FILE_NAME, JSON_FILE_CONTENT)]
		public async Task Test_FileExists_WithContent(string endpoint, string expectedText)
		{
			var client = _webApplicationFactory.CreateClient();
			var response = await client.GetAsync(endpoint);

			Assert.True(response.IsSuccessStatusCode, response.StatusCode.ToString());

			var content = await response.Content.ReadAsStringAsync();
			Assert.Contains(expectedText, content);
		}
	}
}