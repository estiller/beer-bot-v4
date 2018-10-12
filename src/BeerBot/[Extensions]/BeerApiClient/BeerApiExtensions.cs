using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient.Models;

namespace BeerBot.BeerApiClient
{
    public static class CustomBeerApiExtensions
    {
        public static Task<IList<Style>> StylesGetByCategoryAsync(this IBeerApi operations, int? categoryId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return operations.StylesGetAsync(categoryId: new[] { categoryId }, cancellationToken: cancellationToken);
        }

        public static Task<IList<Beer>> BeersGetByStyleAsync(this IBeerApi operations, int? styleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return operations.BeersGetAsync(styleId: new[] { styleId }, cancellationToken: cancellationToken);
        }

        public static Task<IList<Beer>> BeersGetByBreweryAsync(this IBeerApi operations, int? breweryId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return operations.BeersGetAsync(breweryId: new[] { breweryId }, cancellationToken: cancellationToken);
        }
        public static Task<IList<Beer>> BeersGetBySearchTermAsync(this IBeerApi operations, string searchTerm, CancellationToken cancellationToken = default(CancellationToken))
        {
            return operations.BeersGetAsync(searchTerm: new[] { searchTerm }, cancellationToken: cancellationToken);
        }

        public static Task<IList<Brewery>> BreweriesGetByCountryAsync(this IBeerApi operations, string country, CancellationToken cancellationToken = default(CancellationToken))
        {
            return operations.BreweriesGetAsync(country: new[] { country }, cancellationToken: cancellationToken);
        }
    }
}