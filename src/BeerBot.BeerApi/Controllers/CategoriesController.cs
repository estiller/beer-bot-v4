using System.Collections.Generic;
using BeerBot.BeerApi.Dal;
using BeerBot.BeerApi.Extensions;
using BeerBot.BeerApi.Model;
using Microsoft.AspNetCore.Mvc;

namespace BeerBot.BeerApi.Controllers
{
    [Route("api/categories")]
    public class CategoriesController : Controller
    {
        private readonly IRepository<Category> _repository;

        public CategoriesController(IRepository<Category> repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IEnumerable<Category> Get([FromQuery(Name = "searchTerm")] string[] searchTerms)
        {
            return _repository.Get()
                .FilterBySearchTerms(searchTerms, c => c.Name);
        }

        [HttpGet("{id}")]
        public Category Get(int id)
        {
            return _repository.GetById(id);
        }
    }
}