using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
using BeerBot.Services;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BeerBot
{
    public class BeerBot : IBot
    {
        private readonly BeerDialogs _beerDialogs;

        public BeerBot(IBeerApi beerService, IImageSearchService imageSearch)
        {
            _beerDialogs = new BeerDialogs(beerService, imageSearch);
        }

        public async Task OnTurn(ITurnContext context)
        {
            switch (context.Activity.Type)
            {
                case ActivityTypes.Message:
                    await HandleMessage(context);
                    if (!context.Responded)
                    {
                        await context.SendActivity("I didn't quite understand what you are saying! You can type \"help\" for more information");
                    }
                    break;
                case ActivityTypes.ConversationUpdate:
                    await GreetAddedMembers(context);
                    break;
            }

        }

        private async Task HandleMessage(ITurnContext context)
        {
            var conversationState = context.GetConversationState<BeerConversationState>();
            DialogContext dc = _beerDialogs.CreateContext(context, conversationState);
            await dc.Continue();

            if (!context.Responded)
            {
                var luisResult = context.Services.Get<RecognizerResult>(LuisRecognizerMiddleware.LuisRecognizerResultKey);
                var topIntent = luisResult?.GetTopScoringIntent();

                switch (topIntent != null ? topIntent.Value.intent.ToLowerInvariant() : null)
                {
                    case "greet":
                        await dc.Begin(BeerDialogs.Dialogs.Greet);
                        break;
                    case "randombeer":
                        await dc.Begin(BeerDialogs.Dialogs.RandomBeer);
                        break;
                    case "recommendbeer":
                        await dc.Begin(BeerDialogs.Dialogs.RecommendBeer);
                        break;
                    case "orderbeer":
                        await dc.Begin(BeerDialogs.Dialogs.OrderBeer);
                        break;
                    case "gethelp":
                        await dc.Begin(BeerDialogs.Dialogs.MainMenu);
                        break;
                    case "bye":
                        await dc.Begin(BeerDialogs.Dialogs.Exit);
                        break;
                    case "none":
                        break;
                    default:
                        await context.SendActivity($"Something must be wrong with my language circuits... {Emoji.Coffee}");
                        break;
                }
            }
        }

        private Task GreetAddedMembers(ITurnContext context)
        {
            var newMember = context.Activity.MembersAdded.FirstOrDefault();
            if (newMember != null && newMember.Id != context.Activity.Recipient.Id && !string.IsNullOrWhiteSpace(newMember.Name))
            {
                return context.SendActivity($"Howdy {newMember.Name}!");
            }

            return Task.CompletedTask;
        }
    }
}
