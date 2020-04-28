using System.ComponentModel.DataAnnotations;

namespace BeerBot.Luis
{
    public class RecognizerOptions
    {
        [Required] public string AppId { get; set; } = null!;
        [Required] public string ApiKey { get; set; } = null!;
        [Required] public string Endpoint { get; set; } = null!;
    }
}