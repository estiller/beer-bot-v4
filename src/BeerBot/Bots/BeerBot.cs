using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace BeerBot.Bots
{
    public class BeerBot : ActivityHandler
    {
        private readonly IBeerApi _beerService;

        public BeerBot(IBeerApi beerService)
        {
            _beerService = beerService;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Text)
            {
                case var text when Regex.IsMatch(text, "^(hi|hello|hola).*", RegexOptions.IgnoreCase):
                    await turnContext.SendActivityAsync("Welcome to your friendly neighborhood bot-tender! How can I help you?", cancellationToken: cancellationToken);
                    break;
                case var text when Regex.IsMatch(text, ".*random.*", RegexOptions.IgnoreCase):
                    var beer = await _beerService.GetRandomBeerAsync(cancellationToken);
                    await turnContext.SendActivityAsync($"You should definitely get a {beer.Name} {Emoji.Beer}", cancellationToken: cancellationToken);
                    break;
                case var text when Regex.IsMatch(text, ".*help.*", RegexOptions.IgnoreCase):
                    await turnContext.SendActivityAsync("You can type 'random' for getting a beer recommendation", cancellationToken: cancellationToken);
                    break;
                case var text when Regex.IsMatch(text, "^(bye|exit|adios).*", RegexOptions.IgnoreCase):
                    await turnContext.SendActivityAsync($"So soon? Oh well. See you later {Emoji.Wave}", cancellationToken: cancellationToken);
                    break;
                default:
                    await turnContext.SendActivityAsync("I didn't quite understand what you are saying! You can type \"help\" for more information",
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync($"Howdy {member.Name ?? "Stranger"}!", cancellationToken: cancellationToken);
                }
            }
        }
    }
}
