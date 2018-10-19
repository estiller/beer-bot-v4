using System;
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
                var model = await _luisRecognizer.RecognizeAsync<BeerBotLuisModel>(turnContext, cancellationToken);
                var topIntent = model.TopIntent().intent;

                switch (topIntent)
                {
                    case BeerBotLuisModel.Intent.Greet:
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.Greet, cancellationToken: cancellationToken);
                        break;
                    case BeerBotLuisModel.Intent.RandomBeer:
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.RandomBeer, cancellationToken: cancellationToken);
                        break;
                    case BeerBotLuisModel.Intent.RecommendBeer:
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.RecommendBeer, cancellationToken: cancellationToken);
                        break;
                    case BeerBotLuisModel.Intent.OrderBeer:
                    {
                        var beerOrderModel = model.Entities.beerorder?.FirstOrDefault();
                        var beerOrder = new BeerOrder
                        {
                            BeerName = beerOrderModel?.beername?.FirstOrDefault(),
                            Chaser = SafeParse<Chaser>(beerOrderModel?.chaser?.FirstOrDefault()?.FirstOrDefault()),
                            Side = SafeParse<SideDish>(beerOrderModel?.sidedish?.FirstOrDefault()?.FirstOrDefault())
                        };
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.OrderBeer, beerOrder, cancellationToken);
                        break;

                        T SafeParse<T>(string value) where T : struct 
                        {
                            if (value == null) return default(T);
                            return Enum.Parse<T>(value, true);
                        }
                    }
                    case BeerBotLuisModel.Intent.GetHelp:
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.MainMenu, cancellationToken: cancellationToken);
                        break;
                    case BeerBotLuisModel.Intent.Bye:
                        await dc.BeginDialogAsync(BeerDialogs.Dialogs.Exit, cancellationToken: cancellationToken);
                        break;
                    case BeerBotLuisModel.Intent.None:
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
