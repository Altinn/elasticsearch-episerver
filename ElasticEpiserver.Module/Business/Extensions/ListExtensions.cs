using System;
using System.Collections.Generic;
using System.Linq;

namespace ElasticEpiserver.Module.Business.Extensions
{
    public static class ListExtensions
    {
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
        }
    }
}