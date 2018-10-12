using System.Collections.Generic;
using System.Text.RegularExpressions;
using BeerBot.BeerApiClient;
using BeerBot.BeerApiClient.Models;
using BeerBot.Emojis;
using BeerBot.Utils;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace BeerBot
{
    public class BeerDialogs : DialogSet
    {
        public static class Dialogs
        {
            public const string MainMenu = "mainMenu";
            public const string Greet = "greet";
            public const string RandomBeer = "randomBeer";
            public const string RecommendBeer = "recommendBeer";
            public const string Exit = "exit";
        }

        private static class Inputs
        {
            public const string Choice = "choicePrompt";
            public const string Text = "text";
        }

        private static DialogMenu DialogMenu { get; } = new DialogMenu(
            ("Get a random beer", new List<string> {"random", "random beer"}, Dialogs.RandomBeer),
            ("Recommend a beer", new List<string> { "recommend", "recommend beer" }, Dialogs.RecommendBeer),
            ("Exit", new List<string> { "bye", "adios" }, Dialogs.Exit));

        public BeerDialogs(IStatePropertyAccessor<DialogState> dialogState, IBeerApi beerService) : base(dialogState)
        {
            Add(new ChoicePrompt(Inputs.Choice));
            Add(new TextPrompt(Inputs.Text));

            Add(new WaterfallDialog(Dialogs.MainMenu, new WaterfallStep[]
            {
                (stepContext, cancellationToken) => stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                {
                    Prompt = MessageFactory.Text("How can I help you?"),
                    RetryPrompt = MessageFactory.Text("Please choose an option"),
                    Choices = DialogMenu.Choices,
                }),
                async (stepContext, cancellationToken) =>
                {
                    var choice = (FoundChoice) stepContext.Result;
                    var dialogId = DialogMenu.GetDialogId(choice.Value);

                    return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
                },
            }));

            Add(new WaterfallDialog(Dialogs.Greet, new WaterfallStep[]
            {
                (stepContext, cancellationToken) => stepContext.PromptAsync(Inputs.Text,
                    new PromptOptions {Prompt = MessageFactory.Text("Welcome to your friendly neighborhood bot-tender! How can I help?")}),
                async (stepContext, cancellationToken) =>
                {
                    var text = (string) stepContext.Result;
                    if (Regex.IsMatch(text, "^(hi|hello|hola).*", RegexOptions.IgnoreCase))
                    {
                        await stepContext.Context.SendActivityAsync("I feel like we already know each other! How can I help?", cancellationToken: cancellationToken);
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }

                    return await stepContext.CancelAllDialogsAsync(cancellationToken);
                },
            }));

            Add(new WaterfallDialog(Dialogs.RandomBeer, new WaterfallStep[]
            {
                async (stepContext, cancellationToken) =>
                {
                    Beer beer = await beerService.BeersRandomGetAsync(cancellationToken);
                    await stepContext.Context.SendActivityAsync($"You should definitely get a {beer.Name}", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync(beer, cancellationToken);
                },
            }));

            Add(new RecommendBeerDialog(Dialogs.RecommendBeer, beerService));

            Add(new WaterfallDialog(Dialogs.Exit, new WaterfallStep[]
            {
                async (stepContext, cancellationToken) =>
                {
                    await stepContext.Context.SendActivityAsync($"So soon? Oh well. See you later {Emoji.Wave}", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                },
            }));
        }
    }
}