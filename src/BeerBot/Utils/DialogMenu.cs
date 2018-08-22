using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Prompts.Choices;

namespace BeerBot.Utils
{
    internal class DialogMenu
    {
        private struct DialogMenuItem
        {
            public DialogMenuItem(Choice choice, string dialogId)
            {
                Choice = choice;
                DialogId = dialogId;
            }

            public Choice Choice { get; }
            public string DialogId { get; }
        }

        private readonly Dictionary<string, DialogMenuItem> _menuItems;

        public DialogMenu(params (string value, List<string> synonyms, string dialogId)[] items)
        {
            _menuItems = new Dictionary<string, DialogMenuItem>();
            foreach (var item in items)
            {
                var choice = new Choice
                {
                    Value = item.value,
                    Synonyms = item.synonyms
                };
                _menuItems.Add(item.value, new DialogMenuItem(choice, item.dialogId));
            }

            Choices = _menuItems.Values.Select(v => v.Choice).ToList();
        }

        public List<Choice> Choices { get; }

        public string GetDialogId(string value)
        {
            return _menuItems[value].DialogId;
        }
    }
}