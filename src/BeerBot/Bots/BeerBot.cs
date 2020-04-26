using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace BeerBot.Bots
{
    public class BeerBot : IBot
    {
        private readonly IBeerApi _beerService;

        public BeerBot(IBeerApi beerService)
        {
            _beerService = beerService;
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
            switch (turnContext.Activity.Text)
            {
                case var text when Regex.IsMatch(text, "^(hi|hello|hola).*", RegexOptions.IgnoreCase):
                    await turnContext.SendActivityAsync("Welcome to your friendly neighborhood bot-tender! How can I help you?", cancellationToken: cancellationToken);
                    break;
                case var text when Regex.IsMatch(text, ".*random.*", RegexOptions.IgnoreCase):
                    var beer = await _beerService.GetRandomBeerAsync(cancellationToken);
                    await turnContext.SendActivityAsync($"You should definitely get a {beer.Name}", cancellationToken: cancellationToken);
                    break;
                case var text when Regex.IsMatch(text, ".*help.*", RegexOptions.IgnoreCase):
                    await turnContext.SendActivityAsync("You can type 'random' for getting a beer recommendation", cancellationToken: cancellationToken);
                    break;
                case var text when Regex.IsMatch(text, "^(bye|exit|adios).*", RegexOptions.IgnoreCase):
                    await turnContext.SendActivityAsync($"So soon? Oh well. See you later {Emoji.Wave}", cancellationToken: cancellationToken);
                    break;
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