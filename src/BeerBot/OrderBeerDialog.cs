using System;
using System.Collections.Generic;
using System.Linq;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Prompts.Choices;
using Microsoft.Recognizers.Text;
using ChoicePrompt = Microsoft.Bot.Builder.Dialogs.ChoicePrompt;
using ConfirmPrompt = Microsoft.Bot.Builder.Dialogs.ConfirmPrompt;
using TextPrompt = Microsoft.Bot.Builder.Dialogs.TextPrompt;

namespace BeerBot
{
    public class OrderBeerDialog : DialogContainer
    {
        public const string Id = "orderBeer";

        public static class InputArgs
        {
            public const string BeerName = "beerName";
        }

        private static class DialogIds
        {
            public const string GetExactBeerName = "getExactBeerName";
        }

        private static class Inputs
        {
            public const string Text = "textPrompt";
            public const string Choice = "choicePrompt";
            public const string Confirm = "confirmPrompt";
        }

        private readonly IBeerApi _beerService;

        public OrderBeerDialog(IBeerApi beerService) : base(Id)
        {
            _beerService = beerService;

            Dialogs.Add(Inputs.Text, new TextPrompt());
            Dialogs.Add(Inputs.Choice, new ChoicePrompt(Culture.English));
            Dialogs.Add(Inputs.Confirm, new ConfirmPrompt(Culture.English));

            AddMainDialog();
            AddGetExactBeerNameDialog();
        }

        private void AddGetExactBeerNameDialog()
        {
            const string usualBeerDialog = "usualBeer";

            Dialogs.Add(usualBeerDialog, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    string usualBeer = dc.Context.GetUserState<UserInfo>().UsualBeer;
                    await dc.Prompt(Inputs.Confirm, $"Would you like your usual {usualBeer}?");
                },
                async (dc, args, next) =>
                {
                    var usualConfirmed = (bool) args["Confirmation"];
                    if (usualConfirmed)
                    {
                        string usualBeer = dc.Context.GetUserState<UserInfo>().UsualBeer;
                        await dc.End(new Dictionary<string, object> {{"Text", usualBeer}});
                    }
                    else
                    {
                        await dc.Prompt(Inputs.Text, "So what can I offer you instead?");
                    }
                },
            });

            Dialogs.Add(DialogIds.GetExactBeerName, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    if (args != null && args.TryGetValue(InputArgs.BeerName, out object beerNameObject))
                    {
                        await next(new Dictionary<string, object> {{"Text", beerNameObject}});
                    }
                    else if (!string.IsNullOrEmpty(dc.Context.GetUserState<UserInfo>().UsualBeer))
                    {
                        await dc.Begin(usualBeerDialog);
                    }
                    else
                    {
                        await dc.Prompt(Inputs.Text, "What beer would you like to order?");
                    }
                },
                async (dc, args, next) =>
                {
                    var beerName = (string) args["Text"];
                    var beers = await _beerService.BeersGetBySearchTermAsync(beerName);
                    switch (beers.Count)
                    {
                        case 0:
                            await dc.Context.SendActivity($"Oops! I haven't found any beer! {Emoji.Disappointed}");
                            await dc.Replace(DialogIds.GetExactBeerName);
                            break;
                        case 1:
                            await dc.End(new Dictionary<string, object> {{InputArgs.BeerName, beers[0].Name}});
                            break;
                        default:
                        {
                            var choices = ChoiceFactory.ToChoices(beers.Random(10).Select(beer => beer.Name).ToList());
                            await dc.Prompt(Inputs.Choice, "I'm not sure which one", new ChoicePromptOptions
                            {
                                Choices = choices,
                                RetryPromptString = "I probably drank too much. I'm not sure which one."
                            });
                            break;
                        }
                    }
                },
                async (dc, args, next) =>
                {
                    var beerChoice = (FoundChoice) args["Value"];
                    await dc.End(new Dictionary<string, object> {{InputArgs.BeerName, beerChoice.Value}});
                }
            });
        }

        private static readonly List<string> PossibleChasers = Enum.GetValues(typeof(Chaser)).Cast<Chaser>().Select(chaser => chaser.ToString()).ToList();
        private static readonly List<string> PossibleSideDishs = Enum.GetValues(typeof(SideDish)).Cast<SideDish>().Select(chaser => chaser.ToString()).ToList();

        private void AddMainDialog()
        {
            const string orderStateEntry = "beerOrder";

            Dialogs.Add(Id, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    dc.ActiveDialog.State[orderStateEntry] = new BeerOrder();
                    if (args != null && args.TryGetValue(InputArgs.BeerName, out object beerName))
                    {
                        await dc.Begin(DialogIds.GetExactBeerName, new Dictionary<string, object> {{InputArgs.BeerName, beerName} });
                    }
                    else
                    {
                        await dc.Begin(DialogIds.GetExactBeerName);
                    }
                },
                async (dc, args, next) =>
                {
                    var beerOrder = (BeerOrder) dc.ActiveDialog.State[orderStateEntry];
                    beerOrder.BeerName = (string) args[InputArgs.BeerName];

                    if (beerOrder.Chaser != 0)
                    {
                        return;
                    }

                    await dc.Prompt(Inputs.Choice, "Which chaser would you like next to your beer?", new ChoicePromptOptions
                    {
                        Choices = ChoiceFactory.ToChoices(PossibleChasers),
                        RetryPromptString = "I probably drank too much. Which chaser would you like next to your beer?"
                    });
                },
                async (dc, args, next) =>
                {
                    var beerOrder = (BeerOrder) dc.ActiveDialog.State[orderStateEntry];
                    if (beerOrder.Chaser == 0)
                    {
                        var chaserChoice = (FoundChoice) args["Value"];
                        beerOrder.Chaser = Enum.Parse<Chaser>(chaserChoice.Value);
                    }

                    if (beerOrder.Side != 0)
                    {
                        return;
                    }

                    await dc.Prompt(Inputs.Choice, "How about something to eat?", new ChoicePromptOptions
                    {
                        Choices = ChoiceFactory.ToChoices(PossibleSideDishs),
                        RetryPromptString = "I probably drank too much. Which side dish would you like next to your beer?"
                    });
                },
                async (dc, args, next) =>
                {
                    var beerOrder = (BeerOrder) dc.ActiveDialog.State[orderStateEntry];
                    if (beerOrder.Side == 0)
                    {
                        var sideDishChoice = (FoundChoice) args["Value"];
                        beerOrder.Side = Enum.Parse<SideDish>(sideDishChoice.Value);
                    }

                    await dc.Prompt(Inputs.Confirm,
                        $"Just to make sure, do you want a {beerOrder.BeerName} beer with {beerOrder.Chaser} and some {beerOrder.Side} on the side?");
                },
                async (dc, args, next) =>
                {
                    var orderConfirmed = (bool) args["Confirmation"];
                    if (orderConfirmed)
                    {
                        var beerOrder = (BeerOrder) dc.ActiveDialog.State[orderStateEntry];
                        dc.Context.GetUserState<UserInfo>().UsualBeer = beerOrder.BeerName;
                        await dc.Context.SendActivity($"Cheers {Emoji.Beers}");
                    }
                    else
                    {
                        await dc.Context.SendActivity($"Maybe I'll get it right next time {Emoji.Confused}");
                    }
                }
            });
        }
    }
}