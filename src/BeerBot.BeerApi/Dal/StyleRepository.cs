using BeerBot.BeerApi.Model;

namespace BeerBot.BeerApi.Dal
{
    internal class StyleRepository : InMemoryRepository<Style>
    {
        public StyleRepository() : base("BeerBot.BeerApi.Data.Styles.csv", s => s.Id)
        {
        }
    }
}