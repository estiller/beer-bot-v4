using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.BeerApiClient.Models;
using BeerBot.Emojis;
using BeerBot.Services;
using BeerBot.Utils;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
//using Activity = Microsoft.Bot.Schema.Activity;

namespace BeerBot
{
    public class RecommendBeerDialog : ComponentDialog
    {
        private static class DialogIds
        {
            public const string Initial = "initial";
            public const string ByCategory = "byCategory";
            public const string ByOrigin = "byOrigin";
            public const string ByName = "byName";
            public const string BeerConfirmation = "beerConfirmation";
        }

        private static class Inputs
        {
            public const string Choice = "choicePrompt";
            public const string Confirm = "confirmPrompt";
        }

        private static DialogMenu RecommendationMenu { get; } = new DialogMenu(
            ("By category", new List<string> { "category", "cat" }, DialogIds.ByCategory),
            ("By origin", new List<string> { "origin", "country" }, DialogIds.ByOrigin),
            ("By name", new List<string> { "name" }, DialogIds.ByName));

        private readonly IBeerApi _beerService;
        private readonly IImageSearchService _imageSearch;

        public RecommendBeerDialog(string id, IBeerApi beerService, IImageSearchService imageSearch) : base(id)
        {
            _beerService = beerService;
            _imageSearch = imageSearch;

            AddDialog(new ChoicePrompt(Inputs.Choice));
            AddDialog(new ConfirmPrompt(Inputs.Confirm));

            AddDialog(new WaterfallDialog(DialogIds.Initial, new WaterfallStep[]
            {
                (stepContext, cancellationToken) => stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                {
                    Prompt = MessageFactory.Text("How would you like me to recommend your beer?"),
                    RetryPrompt = MessageFactory.Text("Not sure I got it. Could you try again?"),
                    Choices = RecommendationMenu.Choices,
                }),
                (stepContext, cancellationToken) =>
                {
                    var choice = (FoundChoice) stepContext.Result;
                    var dialogId = RecommendationMenu.GetDialogId(choice.Value);

                    return stepContext.BeginDialogAsync(dialogId, cancellationToken: cancellationToken);
                },
            }));
            InitialDialogId = DialogIds.Initial;

            AddRecommendByNameDialog();
            AddRecommendByOriginDialog();
            AddRecommendByCategoryDialog();
            AddBeerConfirmationDialog();
        }

        private void AddRecommendByNameDialog()
        {
            const string beerNamePrompt = "beerNamePrompt";
            AddDialog(new BeerNamePrompt(beerNamePrompt, _beerService));

            AddDialog(new WaterfallDialog(DialogIds.ByName, new WaterfallStep[]
            {
                (stepContext, cancellationToken) => 
                    stepContext.PromptAsync(beerNamePrompt, new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Do you remember the name? Give me what you remember")
                    }, cancellationToken),
                (stepContext, cancellationToken) =>
                {
                    var beers = (IList<Beer>) stepContext.Result;
                    return stepContext.BeginDialogAsync(DialogIds.BeerConfirmation, beers, cancellationToken);
                }
            }));
        }

        private void AddRecommendByOriginDialog()
        {
            const string breweriesStateEntry = "breweryList";

            AddDialog(new WaterfallDialog(DialogIds.ByOrigin, new WaterfallStep[]
            {
                async (stepContext, cancellationToken) =>
                {
                    var countries = await _beerService.BreweriesCountriesGetAsync(cancellationToken);
                    return await stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Where would you like your beer from?"),
                        RetryPrompt = MessageFactory.Text("I probably drank too much. Where would you like your beer from?"),
                        Choices = ChoiceFactory.ToChoices(countries.Random(5)),
                    }, cancellationToken);
                },
                async (stepContext, cancellationToken) =>
                {
                    var countryChoice = (FoundChoice) stepContext.Result;
                    var breweries = (await _beerService.BreweriesGetByCountryAsync(countryChoice.Value, cancellationToken)).Random(5);
                    stepContext.Values[breweriesStateEntry] = breweries;

                    Debug.Assert(breweries.Count > 0, "There is no country in the list with zero breweries!");
                    if (breweries.Count == 1)
                    {
                        await stepContext.Context.SendActivityAsync($"Then you need a beer made by '{breweries[0].Name}'!", cancellationToken: cancellationToken);
                        return await stepContext.NextAsync(new FoundChoice {Value = breweries[0].Name, Index = 0, Score = 1}, cancellationToken);
                    }

                    return await stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Which brewery?"),
                        RetryPrompt = MessageFactory.Text("I probably drank too much. Which brewery was it?"),
                        Choices = ChoiceFactory.ToChoices(breweries.Select(brewery => brewery.Name).ToList()),
                    }, cancellationToken);
                },
                async (stepContext, cancellationToken) =>
                {
                    var breweryChoice = (FoundChoice) stepContext.Result;
                    var breweries = (IList<Brewery>) stepContext.Values[breweriesStateEntry];

                    var beers = (await _beerService.BeersGetByBreweryAsync(breweries[breweryChoice.Index].Id, cancellationToken)).Random(3);
                    return await stepContext.BeginDialogAsync(DialogIds.BeerConfirmation, beers, cancellationToken);
                }
            }));
        }

        private void AddRecommendByCategoryDialog()
        {
            const string categoriesStateEntry = "categoryList";
            const string stylesStateEntry = "styleList";

            AddDialog(new WaterfallDialog(DialogIds.ByCategory, new WaterfallStep[]
            {
                async (stepContext, cancellationToken) =>
                {
                    var categories = await _beerService.CategoriesGetAsync(cancellationToken: cancellationToken);
                    stepContext.Values[categoriesStateEntry] = categories;
                    return await stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Which kind of beer do you like?"),
                        RetryPrompt = MessageFactory.Text("I probably drank too much. Which beer type was it?"),
                        Choices = ChoiceFactory.ToChoices(categories.Select(category => category.Name).ToList()),
                    }, cancellationToken);
                },
                async (stepContext, cancellationToken) =>
                {
                    var categoryChoice = (FoundChoice) stepContext.Result;
                    var categories = (IList<Category>) stepContext.Values[categoriesStateEntry];
                    var styles = await _beerService.StylesGetByCategoryAsync(categories[categoryChoice.Index].Id, cancellationToken);
                    stepContext.Values[stylesStateEntry] = styles;

                    return await stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Which style?"),
                        RetryPrompt = MessageFactory.Text("I probably drank too much. Which style was it?"),
                        Choices = ChoiceFactory.ToChoices(styles.Select(style => style.Name).ToList()),
                    }, cancellationToken);
                },
                async (stepContext, cancellationToken) =>
                {
                    var styleChoice = (FoundChoice) stepContext.Result;
                    var styles = (IList<Style>) stepContext.Values[stylesStateEntry];

                    var beers = await _beerService.BeersGetByStyleAsync(styles[styleChoice.Index].Id, cancellationToken);
                    return await stepContext.BeginDialogAsync(DialogIds.BeerConfirmation, beers, cancellationToken);
                }
            }));
        }

        private void AddBeerConfirmationDialog()
        {
            const string beersKey = "beers";
            const string chosenBeerIndexKey = "chosenBeerIndex";

            AddDialog(new WaterfallDialog(DialogIds.BeerConfirmation, new WaterfallStep[]
            {
                async (stepContext, cancellationToken) =>
                {
                    var beers = (IList<Beer>) stepContext.Options;
                    switch (beers.Count)
                    {
                        case 0:
                            await stepContext.Context.SendActivityAsync($"Oops! I haven't found any beer! Better luck next time {Emoji.Four_Leaf_Clover}", cancellationToken: cancellationToken);
                            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                        case 1:
                            await SendBeerCardAsync(beers[0].Name, stepContext.Context, cancellationToken);
                            return await stepContext.EndDialogAsync(beers[0], cancellationToken);
                        default:
                        {
                            beers = beers.Random(3);
                            stepContext.Values[beersKey] = beers;
                            var choices = ChoiceFactory.ToChoices(beers.Select(beer => beer.Name).ToList());
                            return await stepContext.PromptAsync(Inputs.Choice, new PromptOptions
                            {
                                Prompt = MessageFactory.Text("Which one of these works?"),
                                RetryPrompt = MessageFactory.Text("I probably drank too much. Which one of these work?"),
                                Choices = choices,
                            }, cancellationToken);
                        }
                    }
                },
                async (stepContext, cancellationToken) =>
                {
                    var choice = (FoundChoice) stepContext.Result;
                    stepContext.Values[chosenBeerIndexKey] = choice.Index;
                    if (choice.Score >= 0.7)
                    {
                        return await stepContext.NextAsync(true, cancellationToken);
                    }

                    return await stepContext.PromptAsync(Inputs.Confirm, new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"Just making sure I got it right. Do you want a '{choice.Value}'?")
                    }, cancellationToken);

                },
                async (stepContext, cancellationToken) =>
                {
                    var beerConfirmed = (bool) stepContext.Result;
                    if (beerConfirmed)
                    {
                        var beers = (IList<Beer>) stepContext.Values[beersKey];
                        var chosenIndex = (int) stepContext.Values[chosenBeerIndexKey];
                        await SendBeerCardAsync(beers[chosenIndex].Name, stepContext.Context, cancellationToken);
                        return await stepContext.EndDialogAsync(beers[chosenIndex], cancellationToken);
                    }

                    await stepContext.Context.SendActivityAsync($"Too bad {Emoji.Disappointed}", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }));

            async Task SendBeerCardAsync(string beerName, ITurnContext turnContext, CancellationToken cancellationToken)
            {
                //var typingActivity = Activity.CreateTypingActivity();
                //await turnContext.SendActivityAsync(typingActivity, cancellationToken);
                await Task.Delay(1000);   // Make it look like we're typing a lot

                var imageUrl = await _imageSearch.SearchImage(beerName);
                var activity = MessageFactory.Attachment(
                    new HeroCard(
                            "Your Beer",
                            beerName,
                            images: new[] { new CardImage(imageUrl.ToString()) }
                        )
                        .ToAttachment());
                await turnContext.SendActivityAsync(activity, cancellationToken);
                await turnContext.SendActivityAsync($"Glad I could help {Emoji.Beer}", cancellationToken: cancellationToken);
            }
        }

        private class BeerNamePrompt : Prompt<IList<Beer>>
        {
            private readonly IBeerApi _beerService;

            public BeerNamePrompt(string dialogId, IBeerApi beerService) : base(dialogId)
            {
                _beerService = beerService;
            }

            protected override async Task OnPromptAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, bool isRetry,
                CancellationToken cancellationToken = new CancellationToken())
            {
                if (turnContext == null) throw new ArgumentNullException(nameof(turnContext));
                if (options == null) throw new ArgumentNullException(nameof(options));

                if (isRetry && options.RetryPrompt != null)
                {
                    await turnContext.SendActivityAsync(options.RetryPrompt, cancellationToken).ConfigureAwait(false);
                }
                else if (options.Prompt != null)
                {
                    await turnContext.SendActivityAsync(options.Prompt, cancellationToken).ConfigureAwait(false);
                }
            }

            protected override async Task<PromptRecognizerResult<IList<Beer>>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options,
                CancellationToken cancellationToken = new CancellationToken())
            {
                if (turnContext == null) throw new ArgumentNullException(nameof(turnContext));

                if (turnContext.Activity.Type != ActivityTypes.Message)
                {
                    return new PromptRecognizerResult<IList<Beer>> {Succeeded = false};
                }

                var message = turnContext.Activity.AsMessageActivity();
                if (message.Text == null)
                {
                    return new PromptRecognizerResult<IList<Beer>> { Succeeded = false };
                }

                return await RecognizeBeers(turnContext, cancellationToken, message.Text);
            }

            private async Task<PromptRecognizerResult<IList<Beer>>> RecognizeBeers(ITurnContext turnContext, CancellationToken cancellationToken, string beerName)
            {
                var result = new PromptRecognizerResult<IList<Beer>>();
                if (beerName.Length <= 2)
                {
                    result.Succeeded = false;
                    await turnContext.SendActivityAsync("Beer name should be at least 3 characters long.", cancellationToken: cancellationToken);
                }
                else
                {
                    var beers = await _beerService.BeersGetBySearchTermAsync(beerName, cancellationToken);
                    if (beers.Count == 0)
                    {
                        result.Succeeded = false;
                        await turnContext.SendActivityAsync("Oops! I haven't found any beer! Please try again.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        result.Succeeded = true;
                        result.Value = beers;
                    }
                }

                return result;
            }
        }
    }
}