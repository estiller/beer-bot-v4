using System;
using System.Collections.Generic;
using System.Linq;
using BeerBot.BeerApi.Dal;
using BeerBot.BeerApi.Extensions;
using BeerBot.BeerApi.Model;
using Microsoft.AspNetCore.Mvc;

namespace BeerBot.BeerApi.Controllers
{
    [Route("api/breweries")]
    public class BreweriesController : Controller
    {
        private readonly IRepository<Brewery> _repository;

        public BreweriesController(IRepository<Brewery> repository)
        {
            _repository = repository;
        }

        [HttpGet(Name = "GetBreweries")]
        public IEnumerable<Brewery> Get(
            [FromQuery(Name = "searchTerm")] string[] searchTerms,
            [FromQuery(Name = "country")] string[] countries)
        {
            return _repository.Get()
                .FilterBySearchTerms(searchTerms, b => b.Name)
                .FilterBySearchTerms(countries, b => b.Country);
        }

        [HttpGet("{id}", Name = "GetBreweryById")]
        public Brewery Get(int id)
        {
            return _repository.GetById(id);
        }

        [HttpGet("countries", Name = "GetBreweriesCountries")]
        public IEnumerable<string> GetCountries()
        {
            return _repository.Get()
                .Where(b => !String.IsNullOrEmpty(b.Country))
                .Select(b => b.Country)
                .Distinct()
                .OrderBy(c => c);
        }
    }
}
