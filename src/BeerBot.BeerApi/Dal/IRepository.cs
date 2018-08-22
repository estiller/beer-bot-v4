using System.Collections.Generic;

namespace BeerBot.BeerApi.Dal
{
    public interface IRepository<out T>
    {
        IEnumerable<T> Get();
        T GetById(int id);
        T GetRandom();
    }
}