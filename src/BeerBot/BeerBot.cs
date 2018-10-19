using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
using BeerBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BeerBot
{
    public class BeerBot : IBot
    {
        private readonly BeerDialogs _beerDialogs;
        private readonly LuisRecognizer _luisRecognizer;

        public BeerBot(BeerBotAccessors accessors, LuisRecognizer luisRecognizer, IBeerApi beerService, IImageSearchService imageSearch)
        {
            _beerDialogs = new BeerDialogs(accessors.DialogState, accessors.UserInfo, beerService, imageSearch);
            _luisRecognizer = luisRecognizer;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.Message:
                    await HandleMessageAsync(turnContext, cancellationToken);
                    if (!turnContext.Responded)
                    {
                        await turnContext.SendActivityAsync(
                            "I didn't quite understand what you are saying! You can type \"help\" for more information",
                            cancellationToken: cancellationToken);
                    }
                    break;
                case ActivityTypes.ConversationUpdate:
                    await GreetAddedMembersAsync(turnContext, cancellationToken);
                    break;
            }

        }

        private async Task HandleMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            DialogContext dc = await _beerDialogs.CreateContextAsync(turnContext, cancellationToken);
            var result = await dc.ContinueDialogAsync(cancellationToken);

            if (result.Status == DialogTurnStatus.Empty || result.Status == DialogTurnStatus.Cancelled)
            {
                var luisResult = await _luisRecognizer.RecognizeAsync(turnContext, cancellationToken);
                var topIntent = luisResult?.GetTopScoringIntent();

                switch (topIntent?.intent.ToLowerInvariant())
                {
                    case "greet":
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.Greet, cancellationToken: cancellationToken);
                        break;
                    case "randombeer":
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.RandomBeer, cancellationToken: cancellationToken);
                        break;
                    case "recommendbeer":
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.RecommendBeer, cancellationToken: cancellationToken);
                        break;
                    case "orderbeer":
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.OrderBeer, cancellationToken: cancellationToken);
                        break;
                    case "gethelp":
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.MainMenu, cancellationToken: cancellationToken);
                        break;
                    case "bye":
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.Exit, cancellationToken: cancellationToken);
                        break;
                    case "none":
                        break;
                    default:
                        await turnContext.SendActivityAsync($"Something must be wrong with my language circuits... {Emoji.Coffee}", cancellationToken: cancellationToken);
                        break;
                }
            }
        }

        private Task GreetAddedMembersAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var newMember = turnContext.Activity.MembersAdded.FirstOrDefault();
            if (newMember != null && newMember.Id != turnContext.Activity.Recipient.Id && !string.IsNullOrWhiteSpace(newMember.Name))
            {
                return turnContext.SendActivityAsync($"Howdy {newMember.Name}!", cancellationToken: cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}
