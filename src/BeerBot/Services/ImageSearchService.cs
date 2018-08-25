using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BeerBot.Services
{
    internal class ImageSearchService : IImageSearchService
    {
        private const string ApiRootUrl = "https://api.cognitive.microsoft.com/bing/v7.0/images/search";

        private readonly HttpClient _httpClient;

        public ImageSearchService(string apiKey)
        {
            _httpClient = CreateHttpClient(apiKey);
        }

        public async Task<Uri> SearchImage(string query)
        {
            var response = await _httpClient.GetAsync($"?q={query}");
            response.EnsureSuccessStatusCode();

            dynamic result = await response.Content.ReadAsAsync<JObject>();
            var url = (string)result.value[0].contentUrl;
            return new Uri(url);
        }

        private static HttpClient CreateHttpClient(string apiKey)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(ApiRootUrl)
            };
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            return client;
        }
    }
}