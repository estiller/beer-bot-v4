using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;

namespace BeerBot.BeerApi.Dal
{
    internal class InMemoryRepository<T> : IRepository<T>
    {
        private readonly T[] _entities;
        private readonly ReadOnlyDictionary<int, T> _entitiesDictionary;

        protected InMemoryRepository(string resourceName, Func<T, int> idSelector)
        {
            _entities = LoadEntities(resourceName, idSelector);
            _entitiesDictionary = new ReadOnlyDictionary<int, T>(_entities.ToDictionary(idSelector));
        }

        public IEnumerable<T> Get()
        {
            return _entities;
        }

        public T GetById(int id)
        {
            return _entitiesDictionary[id];
        }

        public T GetRandom()
        {
            var random = new Random();
            return _entities[random.Next(_entities.Length)];
        }

        private static T[] LoadEntities(string resourceName, Func<T, int> idSelector)
        {
            using var stream = typeof(InMemoryRepository<>).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
            Debug.Assert(stream != null);
            
            using var reader = new StreamReader(stream);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Configuration.BadDataFound = null;
            return csv.GetRecords<T>().OrderBy(idSelector).ToArray();
        }
    }
}