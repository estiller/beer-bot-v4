using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.Dialogs.BeerRecommendation;
using BeerBot.Emojis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private static readonly DialogMenu<Intent> MainMenu = DialogMenu<Intent>.Create(
            (new Choice("Get a random beer") {Synonyms = new List<string> {"random", "random beer"}}, Intent.RandomBeer),
            (new Choice("Recommend a beer") { Synonyms = new List<string> { "recommend", "recommend beer" }}, Intent.RecommendBeer),
            (new Choice("Exit") {Synonyms = new List<string> {"bye", "adios"}}, Intent.Bye)
        );

    public MainDialog(RandomBeerDialog randomBeerDialog, RecommendBeerDialog recommendBeerDialog) : base(nameof(MainDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(randomBeerDialog);
            AddDialog(recommendBeerDialog);

            AddDialog(new WaterfallDialog(nameof(MainDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ShowMenuAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(MainDialog);
        }

        private Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var message = stepContext.Options?.ToString() ?? "Welcome to your friendly neighborhood bot-tender! How can I help?";
            var promptMessage = MessageFactory.Text(message, message, InputHints.ExpectingInput);
            return stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private Task<DialogTurnResult> ShowMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Intent intent = GetIntent((string) stepContext.Result);
            if (intent != Intent.GetHelp)
            {
                return stepContext.NextAsync(intent, cancellationToken);
            }

            const string message = "You can choose from one of the following options";
            var prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput);
            return stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions {Choices = MainMenu.Choices, Prompt = prompt}, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Intent intent = stepContext.Result switch
            {
                Intent intentResult => intentResult,
                FoundChoice choice => MainMenu.GetEntryResult(choice.Value),
                _ => throw new Exception($"Previous step provided an unexpected result of type '{stepContext.Result.GetType().FullName}'")
            };

            switch (intent)
            {
                case Intent.Greet:
                    return await stepContext.ReplaceDialogAsync(nameof(MainDialog), "I feel like we already know each other! How can I help?", cancellationToken);

                case Intent.GetHelp:
                    const string message = "You can type 'help' for more information";
                    await stepContext.Context.SendActivityAsync(message, message, InputHints.IgnoringInput, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(MainDialog), "So how can I help?", cancellationToken);

                case Intent.RandomBeer:
                    return await stepContext.BeginDialogAsync(nameof(RandomBeerDialog), null, cancellationToken);

                case Intent.RecommendBeer:
                    return await stepContext.BeginDialogAsync(nameof(RecommendBeerDialog), null, cancellationToken);

                case Intent.Bye:
                    const string baseMessage = "So soon? Oh well. See you later!";
                    await stepContext.Context.SendActivityAsync($"{baseMessage} {Emoji.Wave}", baseMessage, InputHints.IgnoringInput, cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);

                default:
                    return await stepContext.ReplaceDialogAsync(nameof(MainDialog), "I didn't quite understand what you are saying! You can type \"help\" for more information.", cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            const string promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }

        private Intent GetIntent(string input)
        {
            return input switch
            {
                { } when Regex.IsMatch(input, "^(hi|hello|hola).*", RegexOptions.IgnoreCase) => Intent.Greet,
                { } when Regex.IsMatch(input, ".*help.*", RegexOptions.IgnoreCase) => Intent.GetHelp,
                { } when Regex.IsMatch(input, ".*menu.*", RegexOptions.IgnoreCase) => Intent.GetHelp,
                { } when Regex.IsMatch(input, ".*random.*", RegexOptions.IgnoreCase) => Intent.RandomBeer,
                { } when Regex.IsMatch(input, ".*recommend.*", RegexOptions.IgnoreCase) => Intent.RecommendBeer,
                { } when Regex.IsMatch(input, "^(bye|exit|adios).*", RegexOptions.IgnoreCase) => Intent.Bye,
                _ => Intent.None
            };
        }
    }
}