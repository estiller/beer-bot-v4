using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient.Models;
using BeerBot.Emojis;
using BeerBot.Utils;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs.BeerRecommendation
{
    public class RecommendBeerDialog : ComponentDialog
    {
        private const string BeersKey = "beers";
        private const string ChosenBeerIndexKey = "chosenBeerIndex";

        private static DialogMenu<string> RecommendationMenu { get; } = DialogMenu<string>.Create(
            (new Choice("By category") {Synonyms = new List<string> {"category", "cat"}}, nameof(RecommendBeerByCategoryDialog)),
            (new Choice("By origin") {Synonyms = new List<string> {"origin", "country"}}, nameof(RecommendBeerByOriginDialog)),
            (new Choice("By name") {Synonyms = new List<string> {"name"}}, nameof(RecommendBeerByNameDialog)));

        public RecommendBeerDialog(RecommendBeerByCategoryDialog byCategory, RecommendBeerByOriginDialog byOrigin, RecommendBeerByNameDialog byName) 
            : base(nameof(RecommendBeerDialog))
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(byCategory);
            AddDialog(byOrigin);
            AddDialog(byName);

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ShowMenu,
                InvokeDialog,
                AskForConfirmation,
                FinalConfirmation,
                SetResult
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private Task<DialogTurnResult> ShowMenu(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            const string promptMessage = "How would you like me to recommend your beer?";
            const string retryPromptMessage = "Not sure I got it. Could you try again?";
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(promptMessage, promptMessage, InputHints.AcceptingInput),
                RetryPrompt = MessageFactory.Text(retryPromptMessage, retryPromptMessage, InputHints.AcceptingInput), 
                Choices = RecommendationMenu.Choices,
            };
            return stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
        }

        private Task<DialogTurnResult> InvokeDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = (FoundChoice) stepContext.Result;
            var dialogId = RecommendationMenu.GetEntryResult(choice.Value);

            return stepContext.BeginDialogAsync(dialogId, cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> AskForConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beers = (IList<Beer>) stepContext.Result;
            switch (beers.Count)
            {
                case 0:
                    const string zeroItemsBaseMessage = "Oops! I haven't found any beer! Better luck next time";
                    await stepContext.Context.SendActivityAsync($"{zeroItemsBaseMessage} {Emoji.Four_Leaf_Clover}", zeroItemsBaseMessage,
                        InputHints.IgnoringInput, cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                case 1:
                    string oneItemFoundMessage = $"Eureka! This is the beer for you: '{beers[0].Name}'";
                    await stepContext.Context.SendActivityAsync(oneItemFoundMessage, oneItemFoundMessage, InputHints.IgnoringInput, cancellationToken);

                    const string oneItemHappyMessageBase = "Glad I could help";
                    await stepContext.Context.SendActivityAsync($"{oneItemHappyMessageBase} {Emoji.Beer}", oneItemHappyMessageBase,
                        InputHints.IgnoringInput, cancellationToken);

                    return await stepContext.EndDialogAsync(beers[0], cancellationToken);
                default:
                {
                    beers = beers.Random(3);
                    stepContext.Values[BeersKey] = beers;
                    
                    const string promptMessage = "Which one of these works?";
                    const string retryPromptMessage = "I probably drank too much. Which one of these work?";
                    var prompt = new PromptOptions
                    {
                        Prompt = MessageFactory.Text(promptMessage, promptMessage, InputHints.AcceptingInput),
                        RetryPrompt = MessageFactory.Text(retryPromptMessage, retryPromptMessage, InputHints.AcceptingInput), 
                        Choices = ChoiceFactory.ToChoices(beers.Select(beer => beer.Name).ToList()),
                    };
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
                }
            }
        }

        private Task<DialogTurnResult> FinalConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = (FoundChoice) stepContext.Result;
            stepContext.Values[ChosenBeerIndexKey] = choice.Index;
            if (choice.Score >= 0.7)
            {
                return stepContext.NextAsync(true, cancellationToken);
            }

            string message = $"Just making sure I got it right. Do you want a '{choice.Value}'?";
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(message, message, InputHints.AcceptingInput)
            };
            return stepContext.PromptAsync(nameof(ConfirmPrompt), prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> SetResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var beerConfirmed = (bool) stepContext.Result;
            if (beerConfirmed)
            {
                var beers = (IList<Beer>) stepContext.Values[BeersKey];
                var chosenIndex = (int) stepContext.Values[ChosenBeerIndexKey];

                const string baseHappyMessage = "Glad I could help";
                await stepContext.Context.SendActivityAsync($"{baseHappyMessage} {Emoji.Beer}", baseHappyMessage, 
                    InputHints.IgnoringInput, cancellationToken);
                return await stepContext.EndDialogAsync(beers[chosenIndex], cancellationToken);
            }

            const string baseSadMessage = "Too bad";
            await stepContext.Context.SendActivityAsync($"{baseSadMessage} {Emoji.Disappointed}", baseSadMessage, 
                InputHints.IgnoringInput, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}