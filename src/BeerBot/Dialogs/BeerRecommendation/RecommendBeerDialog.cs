using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient.Models;
using BeerBot.Emojis;
using BeerBot.Services.ImageSearch;
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

        private readonly IImageSearch _imageSearch;

        public RecommendBeerDialog(RecommendBeerByCategoryDialog byCategory, RecommendBeerByOriginDialog byOrigin, RecommendBeerByNameDialog byName,
            IImageSearch imageSearch) 
            : base(nameof(RecommendBeerDialog))
        {
            _imageSearch = imageSearch;

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
                Prompt = MessageFactory.Text(promptMessage, promptMessage, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(retryPromptMessage, retryPromptMessage, InputHints.ExpectingInput), 
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
                    await SendBeerCardAsync(beers[0].Name, stepContext.Context, cancellationToken);
                    return await stepContext.EndDialogAsync(beers[0], cancellationToken);
                default:
                {
                    beers = beers.Random(3);
                    stepContext.Values[BeersKey] = beers;
                    
                    const string promptMessage = "Which one of these works?";
                    const string retryPromptMessage = "I probably drank too much. Which one of these work?";
                    var prompt = new PromptOptions
                    {
                        Prompt = MessageFactory.Text(promptMessage, promptMessage, InputHints.ExpectingInput),
                        RetryPrompt = MessageFactory.Text(retryPromptMessage, retryPromptMessage, InputHints.ExpectingInput), 
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
                Prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput)
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

                await SendBeerCardAsync(beers[chosenIndex].Name, stepContext.Context, cancellationToken);
                return await stepContext.EndDialogAsync(beers[chosenIndex], cancellationToken);
            }

            const string baseSadMessage = "Too bad";
            await stepContext.Context.SendActivityAsync($"{baseSadMessage} {Emoji.Disappointed}", baseSadMessage, 
                InputHints.IgnoringInput, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task SendBeerCardAsync(string beerName, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var typingActivity = Activity.CreateTypingActivity();
            await turnContext.SendActivityAsync(typingActivity, cancellationToken);
            var minimalDelayTask = Task.Delay(1500, cancellationToken);   // Make it look like we're typing a lot

            var imageUrlTask = _imageSearch.SearchImage($"{beerName} beer");
            await Task.WhenAll(minimalDelayTask, imageUrlTask);

            const string title = "Your Beer";
            var activity = MessageFactory.Attachment(
                new HeroCard(
                        title,
                        beerName,
                        images: new[] {new CardImage(imageUrlTask.Result.ToString())}
                    )
                    .ToAttachment(), null, title, InputHints.IgnoringInput);
            await turnContext.SendActivityAsync(activity, cancellationToken);

            const string messageBase = "Glad I could help";
            await turnContext.SendActivityAsync($"{messageBase} {Emoji.Beer}", messageBase, InputHints.IgnoringInput, cancellationToken);
        }
    }
}