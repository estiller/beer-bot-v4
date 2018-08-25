using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.BeerApiClient.Models;
using BeerBot.Emojis;
using BeerBot.Utils;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Prompts;
using Microsoft.Bot.Builder.Prompts.Choices;
using Microsoft.Recognizers.Text;
using ChoicePrompt = Microsoft.Bot.Builder.Dialogs.ChoicePrompt;
using ConfirmPrompt = Microsoft.Bot.Builder.Dialogs.ConfirmPrompt;
using TextPrompt = Microsoft.Bot.Builder.Dialogs.TextPrompt;

namespace BeerBot
{
    public class RecommendBeerDialog : DialogContainer
    {
        public const string Id = "recommendBeer";

        public static class OutputArgs
        {
            public const string RecommendedBeerName = "recommendedBeerName";
        }

        private static class DialogIds
        {
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

        private static class ArgNames
        {
            public const string Beers = "beers";
        }

        private static DialogMenu RecommendationMenu { get; } = new DialogMenu(
            ("By category", new List<string> { "category", "cat" }, DialogIds.ByCategory),
            ("By origin", new List<string> { "origin", "country" }, DialogIds.ByOrigin),
            ("By name", new List<string> { "name" }, DialogIds.ByName));

        private readonly IBeerApi _beerService;

        public RecommendBeerDialog(IBeerApi beerService) : base(Id)
        {
            _beerService = beerService;

            Dialogs.Add(Inputs.Choice, new ChoicePrompt(Culture.English));
            Dialogs.Add(Inputs.Confirm, new ConfirmPrompt(Culture.English));

            Dialogs.Add(Id, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    await dc.Prompt(Inputs.Choice, "How would you like me to recommend your beer?", new ChoicePromptOptions
                    {
                        Choices = RecommendationMenu.Choices,
                        RetryPromptString = "Not sure I got it. Could you try again?"
                    });
                },
                async (dc, args, next) =>
                {
                    var choice = (FoundChoice)args["Value"];
                    var dialogId = RecommendationMenu.GetDialogId(choice.Value);

                    await dc.Begin(dialogId, dc.ActiveDialog.State);
                },
                async (dc, args, next) =>
                {
                    if (args != null && args.TryGetValue(OutputArgs.RecommendedBeerName, out object recommendBeerName))
                    {
                        await dc.End(new Dictionary<string, object> {{OutputArgs.RecommendedBeerName, recommendBeerName}});
                    }
                }
            });

            AddRecommendByNameDialog();
            AddRecommendByOriginDialog();
            AddRecommendByCategoryDialog();
            AddBeerConfirmationDialog();
        }

        private void AddRecommendByNameDialog()
        {
            const string beerPrompt = "beerPrompt";
            Dialogs.Add(beerPrompt, new TextPrompt(BeerNameValidator));

            Dialogs.Add(DialogIds.ByName, new WaterfallStep[]
            {
                async (dc, args, next) => { await dc.Prompt(beerPrompt, "Do you remember the name? Give me what you remember"); },
                async (dc, args, next) =>
                {
                    var beers = (IList<Beer>) args[ArgNames.Beers];
                    await dc.Replace(DialogIds.BeerConfirmation, new Dictionary<string, object> {{ArgNames.Beers, beers}});
                }
            });

            async Task BeerNameValidator(ITurnContext context, TextResult result)
            {
                if (result.Value.Length <= 2)
                {
                    result.Status = PromptStatus.NotRecognized;
                    await context.SendActivity("Beer name should be at least 3 characters long.");
                    return;
                }

                var beers = await _beerService.BeersGetBySearchTermAsync(result.Value);
                if (beers.Count == 0)
                {
                    result.Status = PromptStatus.NotRecognized;
                    await context.SendActivity("Oops! I haven't found any beer!");
                    return;
                }

                result[ArgNames.Beers] = beers;
            }
        }

        private void AddRecommendByOriginDialog()
        {
            const string breweriesStateEntry = "breweryList";

            Dialogs.Add(DialogIds.ByOrigin, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    var countries = await _beerService.BreweriesCountriesGetAsync();
                    await dc.Prompt(Inputs.Choice, "Where would you like your beer from?", new ChoicePromptOptions
                    {
                        Choices = ChoiceFactory.ToChoices(countries.Random(5)),
                        RetryPromptString = "I probably drank too much. Where would you like your beer from?"
                    });
                },
                async (dc, args, next) =>
                {
                    var countryChoice = (FoundChoice) args["Value"];
                    var breweries = (await _beerService.BreweriesGetByCountryAsync(countryChoice.Value)).Random(5);
                    dc.ActiveDialog.State[breweriesStateEntry] = breweries;

                    Debug.Assert(breweries.Count > 0, "There is no country in the list with zero breweries!");
                    if (breweries.Count == 1)
                    {
                        await dc.Context.SendActivity($"Then you need a beer made by '{breweries[0].Name}'!");
                        await next(new Dictionary<string, object>
                        {
                            {"Value", new FoundChoice {Value = breweries[0].Name, Index = 0, Score = 1}}
                        });
                        return;
                    }

                    await dc.Prompt(Inputs.Choice, "Which brewery?", new ChoicePromptOptions
                    {
                        Choices = ChoiceFactory.ToChoices(breweries.Select(brewery => brewery.Name).ToList()),
                        RetryPromptString = "I probably drank too much. Which brewery was it?"
                    });
                },
                async (dc, args, next) =>
                {
                    var breweryChoice = (FoundChoice) args["Value"];
                    var breweries = (IList<Brewery>) dc.ActiveDialog.State[breweriesStateEntry];

                    var beers = (await _beerService.BeersGetByBreweryAsync(breweries[breweryChoice.Index].Id)).Random(3);
                    await dc.Replace(DialogIds.BeerConfirmation, new Dictionary<string, object> {{ArgNames.Beers, beers}});
                }
            });
        }

        private void AddRecommendByCategoryDialog()
        {
            const string categoriesStateEntry = "categoryList";
            const string stylesStateEntry = "styleList";

            Dialogs.Add(DialogIds.ByCategory, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    var categories = await _beerService.CategoriesGetAsync();
                    dc.ActiveDialog.State[categoriesStateEntry] = categories;
                    await dc.Prompt(Inputs.Choice, "Which kind of beer do you like?", new ChoicePromptOptions
                    {
                        Choices = ChoiceFactory.ToChoices(categories.Select(category => category.Name).ToList()),
                        RetryPromptString = "I probably drank too much. Which beer type was it?"
                    });
                },
                async (dc, args, next) =>
                {
                    var categoryChoice = (FoundChoice) args["Value"];
                    var categories = (IList<Category>) dc.ActiveDialog.State[categoriesStateEntry];
                    var styles = await _beerService.StylesGetByCategoryAsync(categories[categoryChoice.Index].Id);
                    dc.ActiveDialog.State[stylesStateEntry] = styles;

                    await dc.Prompt(Inputs.Choice, "Which style?", new ChoicePromptOptions
                    {
                        Choices = ChoiceFactory.ToChoices(styles.Select(style => style.Name).ToList()),
                        RetryPromptString = "I probably drank too much. Which style was it?"
                    });
                },
                async (dc, args, next) =>
                {
                    var styleChoice = (FoundChoice) args["Value"];
                    var styles = (IList<Style>) dc.ActiveDialog.State[stylesStateEntry];

                    var beers = await _beerService.BeersGetByStyleAsync(styles[styleChoice.Index].Id);
                    await dc.Replace(DialogIds.BeerConfirmation, new Dictionary<string, object> {{ArgNames.Beers, beers}});
                }
            });
        }

        private void AddBeerConfirmationDialog()
        {
            Dialogs.Add(DialogIds.BeerConfirmation, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    var beers = (IList<Beer>) args[ArgNames.Beers];
                    switch (beers.Count)
                    {
                        case 0:
                            await dc.Context.SendActivity($"Oops! I haven't found any beer! Better luck next time {Emoji.Four_Leaf_Clover}");
                            await dc.End();
                            break;
                        case 1:
                            await dc.Context.SendActivity($"Eureka! This is the beer for you: '{beers[0].Name}'");
                            await dc.Context.SendActivity($"Glad I could help {Emoji.Beer}");
                            await dc.End(new Dictionary<string, object> {{OutputArgs.RecommendedBeerName, beers[0].Name}});
                            break;
                        default:
                        {
                            var choices = ChoiceFactory.ToChoices(beers.Random(3).Select(beer => beer.Name).ToList());
                            await dc.Prompt(Inputs.Choice, "Which one of these works?", new ChoicePromptOptions
                            {
                                Choices = choices,
                                RetryPromptString = "I probably drank too much. Which one of these work?"
                            });
                            break;
                        }
                    }
                },
                async (dc, args, next) =>
                {
                    var choice = (FoundChoice) args["Value"];
                    dc.ActiveDialog.State[OutputArgs.RecommendedBeerName] = choice.Value;
                    if (choice.Score < 0.7)
                    {
                        await dc.Prompt(Inputs.Confirm, $"Just making sure I got it right. Do you want a '{choice.Value}'?");
                    }
                    else
                    {
                        await next(new Dictionary<string, object> {{"Confirmation", true}});
                    }
                },
                async (dc, args, next) =>
                {
                    var beerConfirmed = (bool) args["Confirmation"];
                    if (beerConfirmed)
                    {
                        await dc.Context.SendActivity($"Glad I could help {Emoji.Beer}");
                        await dc.End(new Dictionary<string, object>
                            {{OutputArgs.RecommendedBeerName, dc.ActiveDialog.State[OutputArgs.RecommendedBeerName]}});
                    }
                    else
                    {
                        await dc.Context.SendActivity($"Too bad {Emoji.Disappointed}");
                    }
                }
            });
        }
    }
}