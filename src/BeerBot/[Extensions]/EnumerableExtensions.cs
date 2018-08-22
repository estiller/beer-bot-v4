using System;
using System.Collections.Generic;

namespace BeerBot
{
    public static class EnumerableExtensions
    {
        public static List<T> Random<T>(this IList<T> source, int numOfItems)
        {
            numOfItems = Math.Min(numOfItems, source.Count);
            var selectedItems = new HashSet<int>();
            var result = new List<T>();
            for (int i = 0; i < numOfItems; i++)
            {
                int random = GetUniqueRandomNumber(source.Count, selectedItems);
                result.Add(source[random]);
            }

            return result;
        }

        private static int GetUniqueRandomNumber(int maxValue, HashSet<int> selectedItems)
        {
            int random;
            do
            {
                random = GetRandomNumber(maxValue);
            } while (selectedItems.Contains(random));
            selectedItems.Add(random);
            return random;
        }

        private static readonly Random RandomGenerator = new Random();

        private static int GetRandomNumber(int maxValue)
        {
            lock (RandomGenerator)
            {
                return RandomGenerator.Next(maxValue);
            }
        }
    }
}