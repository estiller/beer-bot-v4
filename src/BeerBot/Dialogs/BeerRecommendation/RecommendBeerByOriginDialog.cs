using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.BeerApiClient.Models;
using BeerBot.Utils;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs.BeerRecommendation
{
    public class RecommendBeerByOriginDialog : ComponentDialog
    {
        private const string BreweriesStateEntry = "breweryList";

        private readonly IBeerApi _beerService;

        public RecommendBeerByOriginDialog(IBeerApi beerService)
            : base(nameof(RecommendBeerByOriginDialog))
        {
            _beerService = beerService;

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptForCountry,
                PromptForBrewery,
                SetResult
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptForCountry(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            const string message = "Where would you like your beer from?";
            const string retryMessage = "I probably drank too much. Where would you like your beer from?";

            var countries = await _beerService.GetBreweriesCountriesAsync(cancellationToken);
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(retryMessage, retryMessage, InputHints.ExpectingInput),
                Choices = ChoiceFactory.ToChoices(countries.Random(5)),
            };
            return await stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForBrewery(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var countryChoice = (FoundChoice)stepContext.Result;
            var breweries = (await _beerService.GetBreweriesByCountryAsync(countryChoice.Value, cancellationToken)).Random(5);
            stepContext.Values[BreweriesStateEntry] = breweries;

            Debug.Assert(breweries.Count > 0, "There is no country in the list with zero breweries!");
            if (breweries.Count == 1)
            {
                string singleBreweryMessage = $"Then you need a beer made by '{breweries[0].Name}'!";
                await stepContext.Context.SendActivityAsync(singleBreweryMessage, singleBreweryMessage, InputHints.IgnoringInput, cancellationToken);
                return await stepContext.NextAsync(new FoundChoice { Value = breweries[0].Name, Index = 0, Score = 1 }, cancellationToken);
            }

            const string promptMessage = "Which brewery?";
            const string retryPromptMessage = "I probably drank too much. Which brewery was it?";
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(promptMessage, promptMessage, InputHints.ExpectingInput), 
                RetryPrompt = MessageFactory.Text(retryPromptMessage, retryPromptMessage, InputHints.ExpectingInput),
                Choices = ChoiceFactory.ToChoices(breweries.Select(brewery => brewery.Name).ToList()),
            };
            return await stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> SetResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var breweryChoice = (FoundChoice)stepContext.Result;
            var breweries = (IList<Brewery>)stepContext.Values[BreweriesStateEntry];

            var beers = (await _beerService.GetBeersByBreweryAsync(breweries[breweryChoice.Index].Id, cancellationToken)).Random(3);
            return await stepContext.EndDialogAsync(beers, cancellationToken);
        }
    }
}