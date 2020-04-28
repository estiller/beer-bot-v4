using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.Dialogs.BeerOrdering;
using BeerBot.Dialogs.BeerRecommendation;
using BeerBot.Emojis;
using BeerBot.Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private static readonly DialogMenu<BeerBotLuisModel.Intent> MainMenu = DialogMenu<BeerBotLuisModel.Intent>.Create(
            (new Choice("Get a random beer") {Synonyms = new List<string> {"random", "random beer"}}, BeerBotLuisModel.Intent.RandomBeer),
            (new Choice("Recommend a beer") { Synonyms = new List<string> { "recommend", "recommend beer" }}, BeerBotLuisModel.Intent.RecommendBeer),
            (new Choice("Exit") {Synonyms = new List<string> {"bye", "adios"}}, BeerBotLuisModel.Intent.Bye)
        );

    private readonly IRecognizer _luisRecognizer;

    public MainDialog(RandomBeerDialog randomBeerDialog, OrderBeerDialog orderBeerDialog, RecommendationConversionDialog recommendationConversionDialog,
        IRecognizer luisRecognizer) 
        : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(randomBeerDialog);
            AddDialog(orderBeerDialog);
            AddDialog(recommendationConversionDialog);

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

        private async Task<DialogTurnResult> ShowMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisModel = await _luisRecognizer.RecognizeAsync<BeerBotLuisModel>(stepContext.Context, cancellationToken);
            if (luisModel.TopIntent().intent != BeerBotLuisModel.Intent.GetHelp)
            {
                return await stepContext.NextAsync(luisModel, cancellationToken);
            }

            const string message = "You can choose from one of the following options";
            var prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions {Choices = MainMenu.Choices, Prompt = prompt}, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            BeerBotLuisModel.Intent intent = stepContext.Result switch
            {
                BeerBotLuisModel model => model.TopIntent().intent,
                FoundChoice choice => MainMenu.GetEntryResult(choice.Value),
                _ => throw new Exception($"Previous step provided an unexpected result of type '{stepContext.Result.GetType().FullName}'")
            };

            switch (intent)
            {
                case BeerBotLuisModel.Intent.Greet:
                    return await stepContext.ReplaceDialogAsync(nameof(MainDialog), "I feel like we already know each other! How can I help?", cancellationToken);

                case BeerBotLuisModel.Intent.GetHelp:
                    const string message = "You can type 'help' for more information";
                    await stepContext.Context.SendActivityAsync(message, message, InputHints.IgnoringInput, cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(MainDialog), "So how can I help?", cancellationToken);

                case BeerBotLuisModel.Intent.RandomBeer:
                    return await stepContext.BeginDialogAsync(nameof(RandomBeerDialog), null, cancellationToken);

                case BeerBotLuisModel.Intent.RecommendBeer:
                    return await stepContext.BeginDialogAsync(nameof(RecommendationConversionDialog), null, cancellationToken);

                case BeerBotLuisModel.Intent.OrderBeer:
                    var luisModel = stepContext.Result as BeerBotLuisModel;
                    BeerOrder? beerOrder = ToBeerOrder(luisModel);
                    return await stepContext.BeginDialogAsync(nameof(OrderBeerDialog), beerOrder, cancellationToken);

                case BeerBotLuisModel.Intent.Bye:
                    const string baseMessage = "So soon? Oh well. See you later!";
                    await stepContext.Context.SendActivityAsync($"{baseMessage} {Emoji.Wave}", baseMessage, InputHints.IgnoringInput, cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);

                default:
                    return await stepContext.ReplaceDialogAsync(nameof(MainDialog), "I didn't quite understand what you are saying! You can type \"help\" for more information.", cancellationToken);
            }

            BeerOrder? ToBeerOrder(BeerBotLuisModel? luisModel)
            {
                if (luisModel == null)
                    return null;
                var beerName = luisModel.Entities?._instance?.beername?.FirstOrDefault()?.Text;
                var chaserName = luisModel.Entities?._instance?.chaser?.FirstOrDefault()?.Text;
                var sideDishName = luisModel.Entities?._instance?.sidedish?.FirstOrDefault()?.Text;

                if (beerName == null && chaserName == null && sideDishName == null)
                    return null;

                return new BeerOrder
                {
                    BeerName = beerName,
                    Chaser = chaserName == null ? (Chaser?) null : Enum.Parse<Chaser>(chaserName, true),
                    Side = sideDishName == null ? (SideDish?) null : Enum.Parse<SideDish>(sideDishName, true)
                };
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            const string promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}