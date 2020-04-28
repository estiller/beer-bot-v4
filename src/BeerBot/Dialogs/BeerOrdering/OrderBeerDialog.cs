using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient.Models;
using BeerBot.Emojis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs.BeerOrdering
{
    public class OrderBeerDialog : ComponentDialog
    {
        private const string OrderStateEntry = "beerOrder";
        private const string IsUsualOrderEntry = "isUsualOrder";

        private static readonly List<string> PossibleChasers =
            Enum.GetValues(typeof(Chaser)).Cast<Chaser>().Select(chaser => chaser.ToString()).ToList();

        private static readonly List<string> PossibleSideDishes =
            Enum.GetValues(typeof(SideDish)).Cast<SideDish>().Select(chaser => chaser.ToString()).ToList();

        private readonly IStatePropertyAccessor<BeerOrder> _lastOrderAccessor;

        public OrderBeerDialog(SearchBeerForOrderDialog searchDialog, UserState userState) 
            : base(nameof(OrderBeerDialog))
        {
            _lastOrderAccessor = userState.CreateProperty<BeerOrder>("LastOrder");

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(searchDialog);

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                CheckUsualOrderStep,
                BeerSelectionStep,
                ChaserSelectionStep,
                SideDishSelectionStep,
                ConfirmationStep,
                FinalStep
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> CheckUsualOrderStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var requestedOrder = (BeerOrder?)stepContext.Options;
            stepContext.Values[OrderStateEntry] = requestedOrder;
            if (requestedOrder != null)
            {
                return await stepContext.NextAsync(false, cancellationToken);
            }

            var lastOrder = await _lastOrderAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);
            if (lastOrder == null) return await stepContext.NextAsync(false, cancellationToken);

            string message = $"Would you like your usual {lastOrder.BeerName} with {lastOrder.Chaser} and some {lastOrder.Side} on the side?";
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput)
            };
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), prompt, cancellationToken);

        }

        private async Task<DialogTurnResult> BeerSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var usualOrderRequested = (bool)stepContext.Result;
            stepContext.Values[IsUsualOrderEntry] = usualOrderRequested;
            if (usualOrderRequested)
            {
                var lastOrder = await _lastOrderAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);
                stepContext.Values[OrderStateEntry] = lastOrder;
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var beerOrder = (BeerOrder) stepContext.Values[OrderStateEntry] ?? new BeerOrder();
            stepContext.Values[OrderStateEntry] = beerOrder;
            return await stepContext.BeginDialogAsync(nameof(SearchBeerForOrderDialog), beerOrder.BeerName, cancellationToken);
        }

        private Task<DialogTurnResult> ChaserSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beerOrder = (BeerOrder) stepContext.Values[OrderStateEntry];
            beerOrder.BeerName = stepContext.Result switch
            {
                Beer beer => beer.Name,
                string beerName => beerName,
                _ => beerOrder.BeerName
            };

            if (beerOrder.BeerName == null)
            {
                return stepContext.EndDialogAsync(null, cancellationToken);
            }

            if (beerOrder.Chaser != null)
            {
                return stepContext.NextAsync(null, cancellationToken);
            }

            const string promptMessage = "Which chaser would you like next to your beer?";
            const string promptRetryMessage = "I probably drank too much. Which chaser would you like next to your beer?";
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(promptMessage, promptMessage, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(promptRetryMessage, promptRetryMessage, InputHints.ExpectingInput),
                Choices = ChoiceFactory.ToChoices(PossibleChasers),
            };
            return stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
        }

        private Task<DialogTurnResult> SideDishSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beerOrder = (BeerOrder) stepContext.Values[OrderStateEntry];
            if (beerOrder.Chaser == null)
            {
                var chaserChoice = (FoundChoice) stepContext.Result;
                beerOrder.Chaser = Enum.Parse<Chaser>(chaserChoice.Value);
            }

            if (beerOrder.Side != null)
            {
                return stepContext.NextAsync(null, cancellationToken);
            }

            const string promptMessage = "How about something to eat?";
            const string promptRetryMessage = "I probably drank too much. Which side dish would you like next to your beer?";
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(promptMessage, promptMessage, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(promptRetryMessage, promptRetryMessage, InputHints.ExpectingInput),
                Choices = ChoiceFactory.ToChoices(PossibleSideDishes)
            };
            return stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
        }

        private Task<DialogTurnResult> ConfirmationStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beerOrder = (BeerOrder) stepContext.Values[OrderStateEntry];
            if (beerOrder.Side == null)
            {
                var sideDishChoice = (FoundChoice) stepContext.Result;
                beerOrder.Side = Enum.Parse<SideDish>(sideDishChoice.Value);
            }

            bool isUsualOrder = (bool) stepContext.Values[IsUsualOrderEntry];
            if (isUsualOrder)
            {
                return stepContext.NextAsync(true, cancellationToken);
            }

            var message = $"Just to make sure, do you want a {beerOrder.BeerName} beer with {beerOrder.Chaser} and some {beerOrder.Side} on the side?";
            var prompt = new PromptOptions {Prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput)};
            return stepContext.PromptAsync(nameof(ConfirmPrompt), prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderConfirmed = (bool) stepContext.Result;
            if (orderConfirmed)
            {
                await stepContext.Context.SendActivityAsync($"Cheers {Emoji.Beers}", "Cheers", 
                    InputHints.IgnoringInput, cancellationToken);

                var beerOrder = (BeerOrder)stepContext.Values[OrderStateEntry];
                await _lastOrderAccessor.SetAsync(stepContext.Context, beerOrder, cancellationToken);
            }
            else
            {
                const string messageBase = "Maybe I'll get it right next time";
                await stepContext.Context.SendActivityAsync($"{messageBase} {Emoji.Confused}", messageBase, 
                    InputHints.IgnoringInput, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}