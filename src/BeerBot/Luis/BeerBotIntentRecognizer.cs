using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Options;

namespace BeerBot.Luis
{
    public class BeerBotIntentRecognizer : IRecognizer
    {
        private readonly LuisRecognizer _recognizer;

        public BeerBotIntentRecognizer(IOptions<RecognizerOptions> options)
        {
            var luisApplication = new LuisApplication(options.Value.AppId, options.Value.ApiKey, options.Value.Endpoint);
            var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication)
            {
                PredictionOptions = new Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions
                {
                    IncludeInstanceData = true
                }
            };
            _recognizer = new LuisRecognizer(recognizerOptions);
        }

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}