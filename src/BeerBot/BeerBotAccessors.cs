using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace BeerBot
{
    public class BeerBotAccessors
    {
        public BeerBotAccessors(ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public static string DialogStateName { get; } = $"{nameof(BeerBotAccessors)}.DialogState";

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public ConversationState ConversationState { get; }
    }
}
