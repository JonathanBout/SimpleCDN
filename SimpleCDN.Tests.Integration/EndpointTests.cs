﻿using System.Net;
using System.Net.Http.Headers;

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
			HttpClient client = _webApplicationFactory.CreateClient();

			HttpResponseMessage response = await client.GetAsync("/");

			Assert.True(response.IsSuccessStatusCode, response.StatusCode.ToString());
		}

		[Fact]
		public async Task Test_AutoGeneratedIndex()
		{
			HttpClient client = _webApplicationFactory.CreateClient();
			HttpResponseMessage response = await client.GetAsync("/");
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
			HttpClient client = _webApplicationFactory.CreateClient();
			HttpResponseMessage response = await client.GetAsync(endpoint);

			Assert.True(response.IsSuccessStatusCode, response.StatusCode.ToString());

			var content = await response.Content.ReadAsStringAsync();
			Assert.Contains(expectedText, content);
		}

		[Theory]
		[InlineData("/test.txt", "text/plain", false)]
		[InlineData("/test.txt", "application/json", true)]
		[InlineData("/data/test.json", "application/json", false)]
		[InlineData("/data/test.json", "text/plain", true)]
		public async Task Test_UnsupportedMediaType_WhenWrongAcceptHeader(string endpoint, string supportedMediaType, bool shouldFail)
		{
			HttpClient client = _webApplicationFactory.CreateClient();
			var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(supportedMediaType));
			HttpResponseMessage response = await client.SendAsync(request);
			if (shouldFail)
			{
				Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
			} else
			{
				Assert.True(response.IsSuccessStatusCode, response.StatusCode.ToString());
			}
		}
	}
}
