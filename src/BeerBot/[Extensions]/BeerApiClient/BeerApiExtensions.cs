using System.Collections.Generic;
using System.Threading.Tasks;
using BeerBot.BeerApiClient.Models;

namespace BeerBot.BeerApiClient
{
    public static class CustomBeerApiExtensions
    {
        public static Task<IList<Style>> StylesGetByCategoryAsync(this IBeerApi operations, int? categoryId)
        {
            return operations.StylesGetAsync(categoryId: new[] { categoryId });
        }

        public static Task<IList<Beer>> BeersGetByStyleAsync(this IBeerApi operations, int? styleId)
        {
            return operations.BeersGetAsync(styleId: new[] { styleId });
        }

        public static Task<IList<Beer>> BeersGetByBreweryAsync(this IBeerApi operations, int? breweryId)
        {
            return operations.BeersGetAsync(breweryId: new[] { breweryId });
        }
        public static Task<IList<Beer>> BeersGetBySearchTermAsync(this IBeerApi operations, string searchTerm)
        {
            return operations.BeersGetAsync(searchTerm: new[] { searchTerm });
        }

        public static Task<IList<Brewery>> BreweriesGetByCountryAsync(this IBeerApi operations, string country)
        {
            return operations.BreweriesGetAsync(country: new[] { country });
        }
    }
}