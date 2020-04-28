using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeerBot.BeerApiClient.Models;

namespace BeerBot.BeerApiClient
{
    public static class CustomBeerApiExtensions
    {
        public static Task<IList<Style>> GetStylesByCategoryAsync(this IBeerApi operations, int? categoryId, CancellationToken cancellationToken = default)
        {
            return operations.GetStylesAsync(categoryId: new[] { categoryId }, cancellationToken: cancellationToken);
        }

        public static Task<IList<Beer>> GetBeersByStyleAsync(this IBeerApi operations, int? styleId, CancellationToken cancellationToken = default)
        {
            return operations.GetBeersAsync(styleId: new[] { styleId }, cancellationToken: cancellationToken);
        }

        public static Task<IList<Beer>> GetBeersByBreweryAsync(this IBeerApi operations, int? breweryId, CancellationToken cancellationToken = default)
        {
            return operations.GetBeersAsync(breweryId: new[] { breweryId }, cancellationToken: cancellationToken);
        }
        public static Task<IList<Beer>> GetBeersBySearchTermAsync(this IBeerApi operations, string searchTerm, CancellationToken cancellationToken = default)
        {
            return operations.GetBeersAsync(searchTerm: new[] { searchTerm }, cancellationToken: cancellationToken);
        }

        public static Task<IList<Brewery>> GetBreweriesByCountryAsync(this IBeerApi operations, string country, CancellationToken cancellationToken = default)
        {
            return operations.GetBreweriesAsync(country: new[] { country }, cancellationToken: cancellationToken);
        }
    }
}