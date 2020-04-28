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

        private static readonly List<string> PossibleChasers =
            Enum.GetValues(typeof(Chaser)).Cast<Chaser>().Select(chaser => chaser.ToString()).ToList();

        private static readonly List<string> PossibleSideDishes =
            Enum.GetValues(typeof(SideDish)).Cast<SideDish>().Select(chaser => chaser.ToString()).ToList();

        public OrderBeerDialog(SearchBeerForOrderDialog searchDialog) 
            : base(nameof(OrderBeerDialog))
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(searchDialog);

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                BeerSelectionStep,
                ChaserSelectionStep,
                SideDishSelectionStep,
                ConfirmationStep,
                FinalStep
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private Task<DialogTurnResult> BeerSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beerOrder = (BeerOrder) stepContext.Options ?? new BeerOrder();
            stepContext.Values[OrderStateEntry] = beerOrder;

            return stepContext.BeginDialogAsync(nameof(SearchBeerForOrderDialog), beerOrder.BeerName, cancellationToken);
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