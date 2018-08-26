using System.Collections.Generic;
using System.Text.RegularExpressions;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
using BeerBot.Services;
using BeerBot.Utils;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Prompts.Choices;
using Microsoft.Recognizers.Text;

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
            public const string OrderBeer = OrderBeerDialog.Id;
            public const string Exit = "exit";
        }

        private class InternalDialogs
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

        public BeerDialogs(IBeerApi beerService, IImageSearchService imageSearch)
        {
            Add(Inputs.Choice, new ChoicePrompt(Culture.English));
            Add(Inputs.Text, new TextPrompt());
            Add(Inputs.Confirm, new ConfirmPrompt(Culture.English));

            Add(Dialogs.MainMenu, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    await dc.Prompt(Inputs.Choice, "How can I help you?", new ChoicePromptOptions
                    {
                        Choices = DialogMenu.Choices,
                        RetryPromptString = "Please choose an option"
                    });
                },
                async (dc, args, next) =>
                {
                    var choice = (FoundChoice)args["Value"];
                    var dialogId = DialogMenu.GetDialogId(choice.Value);

                    await dc.Begin(dialogId, dc.ActiveDialog.State);
                },
            });

            Add(Dialogs.Greet, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    await dc.Prompt(Inputs.Text, "Welcome to your friendly neighborhood bot-tender! How can I help?");
                },
                async (dc, args, next) =>
                {
                    var text = (string)args["Text"];
                    if (Regex.IsMatch(text, "^(hi|hello|hola).*", RegexOptions.IgnoreCase))
                    {
                        await dc.Context.SendActivity("I feel like we already know each other! How can I help?");
                    }
                },
            });

            Add(Dialogs.RandomBeer, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    var beer = await beerService.BeersRandomGetAsync();
                    await dc.Context.SendActivity($"You should definitely get a {beer.Name}");
                },
            });

            Add(InternalDialogs.InternalRecommendBeer, new RecommendBeerDialog(beerService, imageSearch));
            Add(Dialogs.RecommendBeer, new WaterfallStep[]
            {
                async (dc, args, next) => { await dc.Begin(InternalDialogs.InternalRecommendBeer); },
                async (dc, args, next) =>
                {
                    if (args != null && args.TryGetValue(RecommendBeerDialog.OutputArgs.RecommendedBeerName, out object recommendBeerName))
                    {
                        dc.ActiveDialog.State[RecommendBeerDialog.OutputArgs.RecommendedBeerName] = recommendBeerName;
                        await dc.Prompt(Inputs.Confirm, "Would you like to make an order?");
                    }
                    else
                    {
                        await dc.End();
                    }
                },
                async (dc, args, next) =>
                {
                    var orderConfirmed = (bool) args["Confirmation"];
                    if (orderConfirmed)
                    {
                        var beerOrder = new BeerOrder
                        {
                            BeerName = (string)dc.ActiveDialog.State[RecommendBeerDialog.OutputArgs.RecommendedBeerName]
                        };
                        await dc.Begin(Dialogs.OrderBeer, new Dictionary<string, object>{{OrderBeerDialog.InputArgs.BeerOrder, beerOrder}});
                    }
                    else
                    {
                        await dc.Context.SendActivity($"Maybe next time {Emoji.Pray}");
                    }
                },
            });

            Add(Dialogs.OrderBeer, new OrderBeerDialog(beerService));

            Add(Dialogs.Exit, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    await dc.Context.SendActivity($"So soon? Oh well. See you later {Emoji.Wave}");
                },
            });
        }
    }
}