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
            public const string OrderBeer = "orderBeer";
            public const string Exit = "exit";
        }

        private static class InternalDialogs
        {
            public const string InternalRecommendBeer = "internalRecommendBeer";
        }

        private static class Inputs
        {
            public const string Choice = "choicePrompt";
            public const string Text = "textPrompt";
            public const string Confirm = "confirmPrompt";
        }

        private static DialogMenu DialogMenu { get; } = new DialogMenu(
            ("Get a random beer", new List<string> {"random", "random beer"}, Dialogs.RandomBeer),
            ("Recommend a beer", new List<string> { "recommend", "recommend beer" }, Dialogs.RecommendBeer),
            ("Order beer", new List<string> { "order", "order beer" }, Dialogs.OrderBeer),
            ("Exit", new List<string> { "bye", "adios" }, Dialogs.Exit));

        public BeerDialogs(IStatePropertyAccessor<DialogState> dialogState, IBeerApi beerService) : base(dialogState)
        {
            Add(new ChoicePrompt(Inputs.Choice));
            Add(new TextPrompt(Inputs.Text));
            Add(new ConfirmPrompt(Inputs.Confirm));

            Add(new WaterfallDialog(Dialogs.MainMenu, new WaterfallStep[]
            {
                (stepContext, cancellationToken) => stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                {
                    Prompt = MessageFactory.Text("How can I help you?"),
                    RetryPrompt = MessageFactory.Text("Please choose an option"),
                    Choices = DialogMenu.Choices,
                }),
                (stepContext, cancellationToken) =>
                {
                    var choice = (FoundChoice) stepContext.Result;
                    var dialogId = DialogMenu.GetDialogId(choice.Value);

                    return stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
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

            const string recommendBeerKey = "recommendedBeer";
            Add(new RecommendBeerDialog(InternalDialogs.InternalRecommendBeer, beerService));
            Add(new WaterfallDialog(Dialogs.RecommendBeer, new WaterfallStep[]
            {
                (stepContext, cancellationToken) => stepContext.BeginDialogAsync(InternalDialogs.InternalRecommendBeer),
                async (stepContext, cancellationToken) =>
                {
                    var recommendedBeer = (Beer) stepContext.Result;
                    if (recommendedBeer == null)
                    {
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }

                    stepContext.Values[recommendBeerKey] = recommendedBeer;
                    return await stepContext.PromptAsync(Inputs.Confirm, new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Would you like to make an order?"),
                    }, cancellationToken);
                },
                async (stepContext, cancellationToken) =>
                {
                    var orderConfirmed = (bool) stepContext.Result;
                    if (orderConfirmed)
                    {
                        var beer = (Beer) stepContext.Values[recommendBeerKey];
                        return await stepContext.BeginDialogAsync(Dialogs.OrderBeer, beer.Name, cancellationToken);
                    }

                    await stepContext.Context.SendActivityAsync($"Maybe next time {Emoji.Pray}", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                },
            }));

            Add(new OrderBeerDialog(Dialogs.OrderBeer, beerService));

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