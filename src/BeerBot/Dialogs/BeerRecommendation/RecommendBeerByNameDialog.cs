using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.BeerApiClient.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs.BeerRecommendation
{
    public class RecommendBeerByNameDialog : ComponentDialog
    {
        public RecommendBeerByNameDialog(IBeerApi beerService)
            : base(nameof(RecommendBeerByNameDialog))
        {
            AddDialog(new BeerNamePrompt(nameof(BeerNamePrompt), beerService));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptUser,
                SetResult
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private Task<DialogTurnResult> PromptUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            const string message = "Do you remember the name? Give me what you remember";
            var prompt = new PromptOptions {Prompt = MessageFactory.Text(message, message, InputHints.AcceptingInput)};
            return stepContext.PromptAsync(nameof(BeerNamePrompt), prompt, cancellationToken);
        }

        private Task<DialogTurnResult> SetResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beers = (IList<Beer>) stepContext.Result;
            return stepContext.EndDialogAsync(beers, cancellationToken);
        }

        private class BeerNamePrompt : Prompt<IList<Beer>>
        {
            private readonly IBeerApi _beerService;

            public BeerNamePrompt(string dialogId, IBeerApi beerService) 
                : base(dialogId)
            {
                _beerService = beerService;
            }

            protected override async Task OnPromptAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, bool isRetry,
                CancellationToken cancellationToken = new CancellationToken())
            {
                if (isRetry && options.RetryPrompt != null)
                {
                    await turnContext.SendActivityAsync(options.RetryPrompt, cancellationToken);
                }
                else if (options.Prompt != null)
                {
                    await turnContext.SendActivityAsync(options.Prompt, cancellationToken);
                }
            }

            protected override async Task<PromptRecognizerResult<IList<Beer>>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, 
                PromptOptions options, CancellationToken cancellationToken = default)
            {
                if (turnContext.Activity.Type != ActivityTypes.Message)
                {
                    return new PromptRecognizerResult<IList<Beer>> { Succeeded = false };
                }

                var message = turnContext.Activity.AsMessageActivity();
                if (message.Text == null)
                {
                    return new PromptRecognizerResult<IList<Beer>> { Succeeded = false };
                }

                return await RecognizeBeers(turnContext, message.Text, cancellationToken);
            }

            private async Task<PromptRecognizerResult<IList<Beer>>> RecognizeBeers(ITurnContext turnContext, string beerName,
                CancellationToken cancellationToken)
            {
                var result = new PromptRecognizerResult<IList<Beer>>();
                if (beerName.Length <= 2)
                {
                    result.Succeeded = false;
                    await turnContext.SendActivityAsync("Beer name should be at least 3 characters long.", cancellationToken: cancellationToken);
                }
                else
                {
                    var beers = await _beerService.GetBeersBySearchTermAsync(beerName, cancellationToken);
                    if (beers.Count == 0)
                    {
                        result.Succeeded = false;
                        await turnContext.SendActivityAsync("Oops! I haven't found any beer! Please try again.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        result.Succeeded = true;
                        result.Value = beers;
                    }
                }

                return result;
            }
        }
    }
}