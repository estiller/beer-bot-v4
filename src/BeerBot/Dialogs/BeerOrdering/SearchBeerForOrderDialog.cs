using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
using BeerBot.Utils;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs.BeerOrdering
{
    public class SearchBeerForOrderDialog : ComponentDialog
    {
        private readonly IBeerApi _beerService;

        public SearchBeerForOrderDialog(IBeerApi beerService)
            : base(nameof(SearchBeerForOrderDialog))
        {
            _beerService = beerService;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStep,
                ActStep,
                SetResult
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private Task<DialogTurnResult> PromptStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beerName = (string) stepContext.Options;
            if (beerName != null)
            {
                return stepContext.NextAsync(beerName, cancellationToken);
            }

            const string message = "What beer would you like to order?";
            var prompt = new PromptOptions {Prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput)};
            return stepContext.PromptAsync(nameof(TextPrompt), prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beerName = (string) stepContext.Result;
            var beers = await _beerService.GetBeersBySearchTermAsync(beerName, cancellationToken);
            switch (beers.Count)
            {
                case 0:
                    const string zeroItemsBaseMessage = "Oops! I haven't found any beer!";
                    await stepContext.Context.SendActivityAsync($"{zeroItemsBaseMessage} {Emoji.Disappointed}", zeroItemsBaseMessage,
                        InputHints.IgnoringInput, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), null, cancellationToken);
                case 1 when beers[0].Name.Equals(beerName, StringComparison.InvariantCultureIgnoreCase):
                    return await stepContext.NextAsync(beers[0].Name, cancellationToken);
                case 1:
                    string oneNonMatchingItemMessage = $"I only got \"{beers[0].Name}\" but that's close enough.";
                    await stepContext.Context.SendActivityAsync(oneNonMatchingItemMessage, oneNonMatchingItemMessage, 
                        InputHints.IgnoringInput, cancellationToken);
                    return await stepContext.NextAsync(beers[0].Name, cancellationToken);
                default:
                {
                    const string multipleItemsMessage = "I'm not sure which one";
                    const string multipleItemsReplyMessage = "I probably drank too much. I'm not sure which one.";
                    var prompt = new PromptOptions
                    {
                        Prompt = MessageFactory.Text(multipleItemsMessage, multipleItemsMessage, InputHints.ExpectingInput),
                        RetryPrompt = MessageFactory.Text(multipleItemsReplyMessage, multipleItemsReplyMessage, InputHints.ExpectingInput),
                        Choices = ChoiceFactory.ToChoices(beers.Random(10).Select(beer => beer.Name).ToList())
                    };
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
                }
            }
        }

        private Task<DialogTurnResult> SetResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string result = stepContext.Result switch
            {
                string beerName => beerName,
                FoundChoice beerChoice => beerChoice.Value,
                _ => throw new Exception("Unexpected result from previous step")
            };
            return stepContext.EndDialogAsync(result, cancellationToken);
        }
    }
}