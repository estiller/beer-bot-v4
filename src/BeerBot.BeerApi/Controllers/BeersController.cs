using System.Collections.Generic;
using BeerBot.BeerApi.Dal;
using BeerBot.BeerApi.Extensions;
using BeerBot.BeerApi.Model;
using Microsoft.AspNetCore.Mvc;

namespace BeerBot.BeerApi.Controllers
{
    [Route("api/beers")]
    public class BeersController : Controller
    {
        private readonly IRepository<Beer> _repository;

        public BeersController(IRepository<Beer> repository)
        {
            _repository = repository;
        }

        [HttpGet(Name = "GetBeers")]
        public IEnumerable<Beer> Get(
            [FromQuery(Name = "searchTerm")] string[] searchTerms,
            [FromQuery(Name = "breweryId")] int[] breweryIds,
            [FromQuery(Name = "categoryId")] int[] categoryIds,
            [FromQuery(Name = "styleId")] int[] styleIds,
            float? minAbv, float? maxAbv)
        {
            return _repository.Get()
                .FilterBySearchTerms(searchTerms, b => b.Name)
                .FilterBy(breweryIds, b => b.BreweryId)
                .FilterBy(categoryIds, b => b.CategoryId)
                .FilterBy(styleIds, b => b.StyleId)
                .FilterByRange(minAbv, maxAbv, b => b.Abv);
        }

        [HttpGet("random", Name = "GetRandomBeer")]
        public Beer GetRandomBeer()
        {
            return _repository.GetRandom();
        }

        [HttpGet("{id}", Name = "GetBeerById")]
        public Beer Get(int id)
        {
            return _repository.GetById(id);
        }
    }
}
