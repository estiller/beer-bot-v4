using BeerBot.BeerApi.Model;

namespace BeerBot.BeerApi.Dal
{
    internal class BeerRepository : InMemoryRepository<Beer>
    {
        public BeerRepository() : base("BeerBot.BeerApi.Data.Beers.csv", b => b.Id)
        {
        }
    }
}