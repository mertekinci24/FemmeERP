using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Utils;

internal static class LinqExtensions
{
    // Builds a dictionary safely, tolerating duplicate keys by taking the last occurrence
    public static Dictionary<TKey, TValue> SafeToDictionary<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
        foreach (var item in source)
        {
            var key = keySelector(item);
            dict[key] = valueSelector(item); // overwrite on duplicate key
        }
        return dict;
    }
}

