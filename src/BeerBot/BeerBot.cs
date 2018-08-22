using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace BeerBot
{
    public class BeerBot : IBot
    {
        private readonly IBeerApi _beerService;

        public BeerBot(IBeerApi beerService)
        {
            _beerService = beerService;
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
            switch (context.Activity.Text)
            {
                case var text when Regex.IsMatch(text, "^(hi|hello|hola).*", RegexOptions.IgnoreCase):
                    await context.SendActivity("Welcome to your friendly neighbourhood bot-tender! How can I help you?");
                    break;
                case var text when Regex.IsMatch(text, ".*random.*", RegexOptions.IgnoreCase):
                    var beer = await _beerService.BeersRandomGetAsync();
                    await context.SendActivity($"You should definitly get a {beer.Name}");
                    break;
                case var text when Regex.IsMatch(text, ".*help.*", RegexOptions.IgnoreCase):
                    await context.SendActivity("You can type 'random' for getting a beer recommendation");
                    break;
                case var text when Regex.IsMatch(text, "^(bye|exit|adios).*", RegexOptions.IgnoreCase):
                    await context.SendActivity("So soon? Oh well. See you later :)");
                    break;
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
