using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly LuisRecognizer _luisRecognizer;
        private readonly BeerDialogs _beerDialogs;

        public BeerBot(LuisRecognizer luisRecognizer, IBeerApi beerService, IImageSearchService imageSearch)
        {
            _luisRecognizer = luisRecognizer;
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
                        await context.SendActivity(
                            "I didn't quite understand what you are saying! You can type \"help\" for more information",
                            "I didn't quite understand what you are saying! You can type \"help\" for more information");
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
                var model = await _luisRecognizer.Recognize<BeerBotLuisModel>(context.Activity.Text, CancellationToken.None);
                var topIntent = model.TopIntent().intent;

                switch (topIntent)
                {
                    case BeerBotLuisModel.Intent.Greet:
                        await dc.Begin(BeerDialogs.Dialogs.Greet);
                        break;
                    case BeerBotLuisModel.Intent.RandomBeer:
                        await dc.Begin(BeerDialogs.Dialogs.RandomBeer);
                        break;
                    case BeerBotLuisModel.Intent.RecommendBeer:
                        await dc.Begin(BeerDialogs.Dialogs.RecommendBeer);
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
                        await dc.Begin(BeerDialogs.Dialogs.OrderBeer,
                            new Dictionary<string, object> {{OrderBeerDialog.InputArgs.BeerOrder, beerOrder}});
                        break;

                        T SafeParse<T>(string value) where T : struct 
                        {
                            if (value == null) return default(T);
                            return Enum.Parse<T>(value, true);
                        }
                    }
                    case BeerBotLuisModel.Intent.GetHelp:
                        await dc.Begin(BeerDialogs.Dialogs.MainMenu);
                        break;
                    case BeerBotLuisModel.Intent.Bye:
                        await dc.Begin(BeerDialogs.Dialogs.Exit);
                        break;
                    case BeerBotLuisModel.Intent.None:
                        break;
                    default:
                        await context.SendActivity(
                            $"Something must be wrong with my language circuits... {Emoji.Coffee}",
                            "Something must be wrong with my language circuits...");
                        break;
                }
            }
        }

        private Task GreetAddedMembers(ITurnContext context)
        {
            var newMember = context.Activity.MembersAdded.FirstOrDefault();
            if (newMember != null && newMember.Id != context.Activity.Recipient.Id && !string.IsNullOrWhiteSpace(newMember.Name))
            {
                return context.SendActivity($"Howdy {newMember.Name}!", $"Howdy {newMember.Name}!");
            }

            return Task.CompletedTask;
        }
    }
}
