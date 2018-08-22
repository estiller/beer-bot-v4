// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace BeerBot.BeerApiClient
{
    using Models;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for BeerApi.
    /// </summary>
    public static partial class BeerApiExtensions
    {
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='searchTerm'>
            /// </param>
            /// <param name='breweryId'>
            /// </param>
            /// <param name='categoryId'>
            /// </param>
            /// <param name='styleId'>
            /// </param>
            /// <param name='minAbv'>
            /// </param>
            /// <param name='maxAbv'>
            /// </param>
            public static IList<Beer> BeersGet(this IBeerApi operations, IList<string> searchTerm = default(IList<string>), IList<int?> breweryId = default(IList<int?>), IList<int?> categoryId = default(IList<int?>), IList<int?> styleId = default(IList<int?>), double? minAbv = default(double?), double? maxAbv = default(double?))
            {
                return operations.BeersGetAsync(searchTerm, breweryId, categoryId, styleId, minAbv, maxAbv).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='searchTerm'>
            /// </param>
            /// <param name='breweryId'>
            /// </param>
            /// <param name='categoryId'>
            /// </param>
            /// <param name='styleId'>
            /// </param>
            /// <param name='minAbv'>
            /// </param>
            /// <param name='maxAbv'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<Beer>> BeersGetAsync(this IBeerApi operations, IList<string> searchTerm = default(IList<string>), IList<int?> breweryId = default(IList<int?>), IList<int?> categoryId = default(IList<int?>), IList<int?> styleId = default(IList<int?>), double? minAbv = default(double?), double? maxAbv = default(double?), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.BeersGetWithHttpMessagesAsync(searchTerm, breweryId, categoryId, styleId, minAbv, maxAbv, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static Beer BeersRandomGet(this IBeerApi operations)
            {
                return operations.BeersRandomGetAsync().GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Beer> BeersRandomGetAsync(this IBeerApi operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.BeersRandomGetWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static Beer BeersByIdGet(this IBeerApi operations, int id)
            {
                return operations.BeersByIdGetAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Beer> BeersByIdGetAsync(this IBeerApi operations, int id, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.BeersByIdGetWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='searchTerm'>
            /// </param>
            /// <param name='country'>
            /// </param>
            public static IList<Brewery> BreweriesGet(this IBeerApi operations, IList<string> searchTerm = default(IList<string>), IList<string> country = default(IList<string>))
            {
                return operations.BreweriesGetAsync(searchTerm, country).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='searchTerm'>
            /// </param>
            /// <param name='country'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<Brewery>> BreweriesGetAsync(this IBeerApi operations, IList<string> searchTerm = default(IList<string>), IList<string> country = default(IList<string>), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.BreweriesGetWithHttpMessagesAsync(searchTerm, country, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static Brewery BreweriesByIdGet(this IBeerApi operations, int id)
            {
                return operations.BreweriesByIdGetAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Brewery> BreweriesByIdGetAsync(this IBeerApi operations, int id, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.BreweriesByIdGetWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static IList<string> BreweriesCountriesGet(this IBeerApi operations)
            {
                return operations.BreweriesCountriesGetAsync().GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<string>> BreweriesCountriesGetAsync(this IBeerApi operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.BreweriesCountriesGetWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='searchTerm'>
            /// </param>
            public static IList<Category> CategoriesGet(this IBeerApi operations, IList<string> searchTerm = default(IList<string>))
            {
                return operations.CategoriesGetAsync(searchTerm).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='searchTerm'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<Category>> CategoriesGetAsync(this IBeerApi operations, IList<string> searchTerm = default(IList<string>), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.CategoriesGetWithHttpMessagesAsync(searchTerm, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static Category CategoriesByIdGet(this IBeerApi operations, int id)
            {
                return operations.CategoriesByIdGetAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Category> CategoriesByIdGetAsync(this IBeerApi operations, int id, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.CategoriesByIdGetWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='searchTerm'>
            /// </param>
            /// <param name='categoryId'>
            /// </param>
            public static IList<Style> StylesGet(this IBeerApi operations, IList<string> searchTerm = default(IList<string>), IList<int?> categoryId = default(IList<int?>))
            {
                return operations.StylesGetAsync(searchTerm, categoryId).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='searchTerm'>
            /// </param>
            /// <param name='categoryId'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<Style>> StylesGetAsync(this IBeerApi operations, IList<string> searchTerm = default(IList<string>), IList<int?> categoryId = default(IList<int?>), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.StylesGetWithHttpMessagesAsync(searchTerm, categoryId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static Style StylesByIdGet(this IBeerApi operations, int id)
            {
                return operations.StylesByIdGetAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Style> StylesByIdGetAsync(this IBeerApi operations, int id, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.StylesByIdGetWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

    }
}