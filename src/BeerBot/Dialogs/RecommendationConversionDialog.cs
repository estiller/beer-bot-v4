using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient.Models;
using BeerBot.Dialogs.BeerOrdering;
using BeerBot.Dialogs.BeerRecommendation;
using BeerBot.Emojis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs
{
    public class RecommendationConversionDialog : ComponentDialog
    {
        private const string RecommendBeerKey = "recommendedBeer";

        public RecommendationConversionDialog(RecommendBeerDialog recommendBeerDialog, OrderBeerDialog orderBeerDialog)
            : base(nameof(RecommendationConversionDialog))
        {
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(recommendBeerDialog);
            AddDialog(orderBeerDialog);

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                RecommendBeerStep,
                PromptToPlaceOrderStep,
                PlaceOrderStep,
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private Task<DialogTurnResult> RecommendBeerStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.BeginDialogAsync(nameof(RecommendBeerDialog), null, cancellationToken);
        }

        private async Task<DialogTurnResult> PromptToPlaceOrderStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var recommendedBeer = (Beer) stepContext.Result;
            if (recommendedBeer == null)
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            stepContext.Values[RecommendBeerKey] = recommendedBeer;
            const string message = "Would you like to make an order?";
            var prompt = new PromptOptions {Prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput)};
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> PlaceOrderStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderConfirmed = (bool) stepContext.Result;
            if (orderConfirmed)
            {
                var beer = (Beer) stepContext.Values[RecommendBeerKey];
                return await stepContext.BeginDialogAsync(nameof(OrderBeerDialog), new BeerOrder {BeerName = beer.Name}, cancellationToken);
            }

            const string messageBase = "Maybe next time";
            await stepContext.Context.SendActivityAsync($"{messageBase} {Emoji.Pray}", messageBase, InputHints.IgnoringInput, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}