using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace BeerBot
{
    public class BeerBotAccessors
    {
        public BeerBotAccessors(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        public static string DialogStateName { get; } = $"{nameof(BeerBotAccessors)}.DialogState";

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public static string UserInfoName { get; } = $"{nameof(BeerBotAccessors)}.UserInfo";

        public IStatePropertyAccessor<UserInfo> UserInfo { get; set; }

        public ConversationState ConversationState { get; }

        public UserState UserState { get; }
    }
}
