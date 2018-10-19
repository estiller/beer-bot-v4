using System;
using System.Collections.Generic;
using System.Linq;
using BeerBot.BeerApiClient;
using BeerBot.Emojis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace BeerBot
{
    public class OrderBeerDialog : ComponentDialog
    {
        private static class DialogIds
        {
            public const string Main = "main";
            public const string GetExactBeerName = "getExactBeerName";
        }

        private static class Inputs
        {
            public const string Text = "textPrompt";
            public const string Choice = "choicePrompt";
            public const string Confirm = "confirmPrompt";
        }

        private readonly IStatePropertyAccessor<UserInfo> _userInfo;
        private readonly IBeerApi _beerService;

        public OrderBeerDialog(string id, IStatePropertyAccessor<UserInfo> userInfo, IBeerApi beerService) : base(id)
        {
            _userInfo = userInfo;
            _beerService = beerService;

            AddDialog(new TextPrompt(Inputs.Text));
            AddDialog(new ChoicePrompt(Inputs.Choice));
            AddDialog(new ConfirmPrompt(Inputs.Confirm));

            AddMainDialog();
            AddGetExactBeerNameDialog();
        }

        private void AddGetExactBeerNameDialog()
        {
            const string usualBeerDialog = "usualBeer";

            AddDialog(new WaterfallDialog(usualBeerDialog, new WaterfallStep[]
            {
                (stepContext, cancellationToken) =>
                {
                    string usualBeer = (string) stepContext.Options;
                    return stepContext.PromptAsync(Inputs.Confirm, new PromptOptions
                    {
                        Prompt = MessageFactory.Text(
                            $"Would you like your usual {usualBeer}?",
                            $"Would you like your usual {usualBeer}?",
                            InputHints.ExpectingInput),
                    }, cancellationToken);
                },
                (stepContext, cancellationToken) =>
                {
                    var usualConfirmed = (bool) stepContext.Result;
                    if (usualConfirmed)
                    {
                        string usualBeer = (string) stepContext.Options;
                        return stepContext.EndDialogAsync(usualBeer, cancellationToken);
                    }

                    return stepContext.PromptAsync(Inputs.Text, new PromptOptions
                    {
                        Prompt = MessageFactory.Text("So what can I offer you instead?", "So what can I offer you instead?", InputHints.ExpectingInput),
                    }, cancellationToken);
                },
                (stepContext, cancellationToken) =>
                {
                    var beerName = (string)stepContext.Result;
                    return stepContext.EndDialogAsync(beerName, cancellationToken);
                },
            }));

            AddDialog(new WaterfallDialog(DialogIds.GetExactBeerName, new WaterfallStep[]
            {
                async (stepContext, cancellationToken) =>
                {
                    var beerName = (string) stepContext.Options;
                    if (beerName != null)
                    {
                        return await stepContext.NextAsync(beerName, cancellationToken);
                    }

                    var userInfo = await _userInfo.GetAsync(stepContext.Context, () => new UserInfo(), cancellationToken);
                    if (!string.IsNullOrEmpty(userInfo.UsualBeer))
                    {
                        return await stepContext.BeginDialogAsync(usualBeerDialog, userInfo.UsualBeer, cancellationToken);
                    }

                    return await stepContext.PromptAsync(Inputs.Text, new PromptOptions
                    {
                        Prompt = MessageFactory.Text("What beer would you like to order?", "What beer would you like to order?", InputHints.ExpectingInput),

                    }, cancellationToken);
                },
                async (stepContext, cancellationToken) =>
                {
                    var beerName = (string) stepContext.Result;
                    var beers = await _beerService.BeersGetBySearchTermAsync(beerName, cancellationToken);
                    switch (beers.Count)
                    {
                        case 0:
                            await stepContext.Context.SendActivityAsync(
                                $"Oops! I haven't found any beer! {Emoji.Disappointed}",
                                "Oops! I haven't found any beer!",
                                cancellationToken: cancellationToken);
                            return await stepContext.ReplaceDialogAsync(DialogIds.GetExactBeerName, cancellationToken: cancellationToken);
                        case 1:
                            return await stepContext.EndDialogAsync(beers[0].Name, cancellationToken);
                        default:
                        {
                            var choices = ChoiceFactory.ToChoices(beers.Random(10).Select(beer => beer.Name).ToList());
                            return await stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                            {
                                Prompt = MessageFactory.Text(
                                    "I'm not sure which one", 
                                    "I'm not sure which one"),
                                RetryPrompt = MessageFactory.Text(
                                    "I probably drank too much. I'm not sure which one.", 
                                    "I probably drank too much. I'm not sure which one."),
                                Choices = choices,
                            }, cancellationToken);
                        }
                    }
                },
                (stepContext, cancellationToken) =>
                {
                    var beerChoice = (FoundChoice) stepContext.Result;
                    return stepContext.EndDialogAsync(beerChoice.Value, cancellationToken);
                }
            }));
        }

        private static readonly List<string> PossibleChasers = Enum.GetValues(typeof(Chaser)).Cast<Chaser>().Select(chaser => chaser.ToString()).ToList();
        private static readonly List<string> PossibleSideDishs = Enum.GetValues(typeof(SideDish)).Cast<SideDish>().Select(chaser => chaser.ToString()).ToList();

        private void AddMainDialog()
        {
            const string orderStateEntry = "beerOrder";

            AddDialog(new WaterfallDialog(DialogIds.Main, new WaterfallStep[]
            {
                (stepContext, cancellationToken) =>
                {
                    var beerOrder = (BeerOrder) stepContext.Options ?? new BeerOrder();
                    stepContext.Values[orderStateEntry] = beerOrder;
                    return stepContext.BeginDialogAsync(DialogIds.GetExactBeerName, beerOrder.BeerName, cancellationToken);
                },
                (stepContext, cancellationToken) =>
                {
                    var beerOrder = (BeerOrder) stepContext.Values[orderStateEntry];
                    beerOrder.BeerName = (string) stepContext.Result;

                    if (beerOrder.Chaser != 0)
                    {
                        return stepContext.NextAsync(cancellationToken: cancellationToken);
                    }

                    return stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                    {
                        Prompt = MessageFactory.Text(
                            "Which chaser would you like next to your beer?", 
                            "Which chaser would you like next to your beer?",
                            InputHints.ExpectingInput),
                        RetryPrompt = MessageFactory.Text(
                            "I probably drank too much. Which chaser would you like next to your beer?",
                            "I probably drank too much. Which chaser would you like next to your beer?",
                            InputHints.ExpectingInput),
                        Choices = ChoiceFactory.ToChoices(PossibleChasers),
                    }, cancellationToken);
                },
                (stepContext, cancellationToken) =>
                {
                    var beerOrder = (BeerOrder) stepContext.Values[orderStateEntry];
                    if (beerOrder.Chaser == 0)
                    {
                        var chaserChoice = (FoundChoice) stepContext.Result;
                        beerOrder.Chaser = Enum.Parse<Chaser>(chaserChoice.Value);
                    }

                    if (beerOrder.Side != 0)
                    {
                        return stepContext.NextAsync(cancellationToken: cancellationToken);
                    }

                    return stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                    {
                        Prompt = MessageFactory.Text(
                            "How about something to eat?", 
                            "How about something to eat?",
                            InputHints.ExpectingInput),
                        RetryPrompt = MessageFactory.Text(
                            "I probably drank too much. Which side dish would you like next to your beer?",
                            "I probably drank too much. Which side dish would you like next to your beer?",
                            InputHints.ExpectingInput),
                        Choices = ChoiceFactory.ToChoices(PossibleSideDishs),
                    }, cancellationToken);
                },
                (stepContext, cancellationToken) =>
                {
                    var beerOrder = (BeerOrder) stepContext.Values[orderStateEntry];
                    if (beerOrder.Side == 0)
                    {
                        var sideDishChoice = (FoundChoice) stepContext.Result;
                        beerOrder.Side = Enum.Parse<SideDish>(sideDishChoice.Value);
                    }

                    return stepContext.PromptAsync(Inputs.Confirm, new PromptOptions
                    {
                        Prompt = MessageFactory.Text(
                            $"Just to make sure, do you want a {beerOrder.BeerName} beer with {beerOrder.Chaser} and some {beerOrder.Side} on the side?",
                            $"Just to make sure, do you want a {beerOrder.BeerName} beer with {beerOrder.Chaser} and some {beerOrder.Side} on the side?",
                            InputHints.ExpectingInput)
                    }, cancellationToken);
                },
                async (stepContext, cancellationToken) =>
                {
                    var orderConfirmed = (bool) stepContext.Result;
                    if (orderConfirmed)
                    {
                        var beerOrder = (BeerOrder) stepContext.Values[orderStateEntry];
                        await _userInfo.SetAsync(stepContext.Context, new UserInfo { UsualBeer = beerOrder.BeerName }, cancellationToken);
                        await stepContext.Context.SendActivityAsync(
                            $"Cheers {Emoji.Beers}",
                            @"<speak version=""1.0"" xmlns=""https://www.w3.org/2001/10/synthesis"" xml:lang=""en-US""><prosody pitch=""high"">Cheers</prosody></speak>", 
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(
                            $"Maybe I'll get it right next time {Emoji.Confused}",
                            "Maybe I'll get it right next time",
                            cancellationToken: cancellationToken);
                    }

                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }));
            InitialDialogId = DialogIds.Main;
        }
    }
}