using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.Services;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
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
                switch (context.Activity.Text)
                {
                    case var text when Regex.IsMatch(text, "^(hi|hello|hola).*", RegexOptions.IgnoreCase):
                        await dc.Begin(BeerDialogs.Dialogs.Greet);
                        break;
                    case var text when Regex.IsMatch(text, ".*random.*", RegexOptions.IgnoreCase):
                        await dc.Begin(BeerDialogs.Dialogs.RandomBeer);
                        break;
                    case var text when Regex.IsMatch(text, ".*recommend.*", RegexOptions.IgnoreCase):
                        await dc.Begin(BeerDialogs.Dialogs.RecommendBeer);
                        break;
                    case var text when Regex.IsMatch(text, ".*order.*", RegexOptions.IgnoreCase):
                        await dc.Begin(BeerDialogs.Dialogs.OrderBeer);
                        break;
                    case var text when Regex.IsMatch(text, ".*help.*", RegexOptions.IgnoreCase):
                        await dc.Begin(BeerDialogs.Dialogs.MainMenu);
                        break;
                    case var text when Regex.IsMatch(text, "^(bye|exit|adios).*", RegexOptions.IgnoreCase):
                        await dc.Begin(BeerDialogs.Dialogs.Exit);
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
