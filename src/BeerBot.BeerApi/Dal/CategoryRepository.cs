using BeerBot.BeerApi.Model;

namespace BeerBot.BeerApi.Dal
{
    internal class CategoryRepository : InMemoryRepository<Category>
    {
        public CategoryRepository() : base("BeerBot.BeerApi.Data.Categories.csv", c => c.Id)
        {
        }
    }
}