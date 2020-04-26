using System.Collections.Generic;
using BeerBot.BeerApi.Dal;
using BeerBot.BeerApi.Extensions;
using BeerBot.BeerApi.Model;
using Microsoft.AspNetCore.Mvc;

namespace BeerBot.BeerApi.Controllers
{
    [Route("api/styles")]
    public class StylesController : Controller
    {
        private readonly IRepository<Style> _repository;

        public StylesController(IRepository<Style> repository)
        {
            _repository = repository;
        }

        [HttpGet(Name = "GetStyles")]
        public IEnumerable<Style> Get(
            [FromQuery(Name = "searchTerm")] string[] searchTerms,
            [FromQuery(Name = "categoryId")] int[] categoryIds)
        {
            return _repository.Get()
                .FilterBySearchTerms(searchTerms, s => s.Name)
                .FilterBy(categoryIds, s => s.CategoryId);
        }

        [HttpGet("{id}", Name = "GetStyleById")]
        public Style Get(int id)
        {
            return _repository.GetById(id);
        }
    }
}