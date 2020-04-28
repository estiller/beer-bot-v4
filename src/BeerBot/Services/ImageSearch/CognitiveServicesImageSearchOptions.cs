using System.ComponentModel.DataAnnotations;

namespace BeerBot.Services.ImageSearch
{
    public class CognitiveServicesImageSearchOptions
    {
        [Required]
        public string EndpointUrl { get; set; } = null!;

        [Required]
        public string ApiKey { get; set; } = null!;
    }
}