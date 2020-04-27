using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace BeerBot.Dialogs
{
    internal class DialogMenu<TEntryResult>
    {
        internal static DialogMenu<TEntryResult> Create(params (Choice choice, TEntryResult result)[] entries)
        {
            IEnumerable<DialogMenuEntry> dialogMenuEntries =
                from entry in entries
                select new DialogMenuEntry(entry.choice, entry.result);

            return new DialogMenu<TEntryResult>(dialogMenuEntries);
        }

        private readonly Dictionary<string, DialogMenuEntry> _menuEntries;

        private DialogMenu(IEnumerable<DialogMenuEntry> entries)
        {
            _menuEntries = entries.ToDictionary(e => e.Choice.Value);
            Choices = _menuEntries.Values.Select(v => v.Choice).ToList();
        }

        public IList<Choice> Choices { get; }

        public TEntryResult GetEntryResult(string value)
        {
            return _menuEntries[value].Result;
        }

        private readonly struct DialogMenuEntry
        {
            internal DialogMenuEntry(Choice choice, TEntryResult result)
            {
                Choice = choice;
                Result = result;
            }

            public Choice Choice { get; }
            public TEntryResult Result { get; }
        }
    }
}