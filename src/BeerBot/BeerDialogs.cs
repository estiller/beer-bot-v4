using System.Collections.Generic;
using System.Text.RegularExpressions;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
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
            public const string RecommendBeer = RecommendBeerDialog.Id;
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

        public BeerDialogs(IBeerApi beerService)
        {
            Add(Inputs.Choice, new ChoicePrompt(Culture.English));
            Add(Inputs.Text, new TextPrompt());

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

            Add(Dialogs.RecommendBeer, new RecommendBeerDialog(beerService));

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