namespace BeerBot.Dialogs.BeerOrdering
{
    public class BeerOrder
    {
        public string? BeerName { get; set; }
        public Chaser? Chaser { get; set; }
        public SideDish? Side { get; set; }
    }

    public enum Chaser
    {
        Whiskey,
        Vodka,
        Liquor,
        Water
    }

    public enum SideDish
    {
        Fries = 1,
        Pretzels,
        Nachos
    }

}