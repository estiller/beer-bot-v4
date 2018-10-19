using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BeerBot.Services
{
    internal class ImageSearchService : IImageSearchService
    {
        private readonly string _rootUrl;
        private readonly HttpClient _httpClient;

        public ImageSearchService(string rootUrl, string apiKey)
        {
            _rootUrl = rootUrl;
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

        private HttpClient CreateHttpClient(string apiKey)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(_rootUrl)
            };
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            return client;
        }
    }
}