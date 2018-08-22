using BeerBot.BeerApi.Model;

namespace BeerBot.BeerApi.Dal
{
    internal class BreweryRepository : InMemoryRepository<Brewery>
    {
        public BreweryRepository() : base("BeerBot.BeerApi.Data.Breweries.csv", b => b.Id)
        {
        }
    }
}