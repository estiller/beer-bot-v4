using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeerBot.Services.ImageSearch
{
    internal class CognitiveServicesImageSearch : IImageSearch
    {
        private readonly HttpClient _httpClient;

        public CognitiveServicesImageSearch(IOptions<CognitiveServicesImageSearchOptions> options)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.Value.EndpointUrl)
            };
            // ReSharper disable once StringLiteralTypo
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.Value.ApiKey);
        }

        public async Task<Uri> SearchImage(string query)
        {
            var response = await _httpClient.GetAsync($"?q={query}");
            response.EnsureSuccessStatusCode();

            var resultString = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject<JObject>(resultString);
            var url = (string)result.value[0].contentUrl;
            return new Uri(url);
        }
    }
}