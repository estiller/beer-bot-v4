using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient;
using BeerBot.BeerApiClient.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace BeerBot.Dialogs.BeerRecommendation
{
    public class RecommendBeerByCategoryDialog : ComponentDialog
    {
        private const string CategoriesStateEntry = "categoryList";
        private const string StylesStateEntry = "styleList";

        private readonly IBeerApi _beerService;

        public RecommendBeerByCategoryDialog(IBeerApi beerService)
            : base(nameof(RecommendBeerByCategoryDialog))
        {
            _beerService = beerService;

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptForCategory,
                PromptForStyle,
                SetResult
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptForCategory(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var categories = await _beerService.GetCategoriesAsync(cancellationToken: cancellationToken);
            stepContext.Values[CategoriesStateEntry] = categories;

            const string message = "Which kind of beer do you like?";
            const string retryMessage = "I probably drank too much. Which beer type was it?";
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(retryMessage, retryMessage, InputHints.ExpectingInput),
                Choices = ChoiceFactory.ToChoices(categories.Select(category => category.Name).ToList()),
            };
            return await stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForStyle(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var categoryChoice = (FoundChoice) stepContext.Result;
            var categories = (IList<Category>) stepContext.Values[CategoriesStateEntry];
            var styles = await _beerService.GetStylesByCategoryAsync(categories[categoryChoice.Index].Id, cancellationToken);
            stepContext.Values[StylesStateEntry] = styles;

            const string message = "Which style?";
            const string retryMessage = "I probably drank too much. Which style was it?";
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(message, message, InputHints.ExpectingInput), 
                RetryPrompt = MessageFactory.Text(retryMessage, retryMessage, InputHints.ExpectingInput),
                Choices = ChoiceFactory.ToChoices(styles.Select(style => style.Name).ToList())
            };
            return await stepContext.PromptAsync(nameof(ChoicePrompt), prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> SetResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var styleChoice = (FoundChoice) stepContext.Result;
            var styles = (IList<Style>) stepContext.Values[StylesStateEntry];

            var beers = await _beerService.GetBeersByStyleAsync(styles[styleChoice.Index].Id, cancellationToken);
            return await stepContext.EndDialogAsync(beers, cancellationToken);
        }
    }
}