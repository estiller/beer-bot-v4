using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.BeerApiClient.Models;
using BeerBot.Emojis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs.BeerRecommendation
{
    public class RandomBeerDialog : ComponentDialog
    {
        private readonly IBeerApi _beerService;

        public RandomBeerDialog(IBeerApi beerService) : base(nameof(RandomBeerDialog))
        {
            _beerService = beerService;
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                OutputRandomBeerAsync
            }));
        }

        private async Task<DialogTurnResult> OutputRandomBeerAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Beer beer = await _beerService.GetRandomBeerAsync(cancellationToken);
            var baseMessage = $"You should definitely get a \"{beer.Name}\"!";
            await stepContext.Context.SendActivityAsync($"{baseMessage} {Emoji.Beer}", baseMessage, InputHints.IgnoringInput, cancellationToken);
            return await stepContext.EndDialogAsync(beer, cancellationToken);
        }
    }
}