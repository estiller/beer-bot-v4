using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace BeerBot.Bots
{
    public class BeerBot : ActivityHandler
    {
        private readonly MainDialog _dialog;
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;

        public BeerBot(MainDialog dialog, ConversationState conversationState, ILogger<BeerBot> logger)
        {
            _dialog = dialog;
            _conversationState = conversationState;
            _logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with Message Activity.");
            return _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    string message = $"Howdy {member.Name ?? "Stranger"}!";
                    await turnContext.SendActivityAsync(message, message, InputHints.IgnoringInput, cancellationToken);
                    await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                }
            }
        }
    }
}
