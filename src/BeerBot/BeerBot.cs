using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BeerBot
{
    public class BeerBot : IBot
    {
        private readonly BeerDialogs _beerDialogs;

        public BeerBot(BeerBotAccessors accessors, IBeerApi beerService)
        {
            _beerDialogs = new BeerDialogs(accessors.DialogState, beerService);
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
                switch (turnContext.Activity.Text)
                {
                    case var text when Regex.IsMatch(text, "^(hi|hello|hola).*", RegexOptions.IgnoreCase):
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.Greet, cancellationToken: cancellationToken);
                        break;
                    case var text when Regex.IsMatch(text, ".*random.*", RegexOptions.IgnoreCase):
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.RandomBeer, cancellationToken: cancellationToken);
                        break;
                    case var text when Regex.IsMatch(text, ".*recommend.*", RegexOptions.IgnoreCase):
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.RecommendBeer, cancellationToken: cancellationToken);
                        break;
                    case var text when Regex.IsMatch(text, ".*help.*", RegexOptions.IgnoreCase):
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.MainMenu, cancellationToken: cancellationToken);
                        break;
                    case var text when Regex.IsMatch(text, "^(bye|exit|adios).*", RegexOptions.IgnoreCase):
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.Exit, cancellationToken: cancellationToken);
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
