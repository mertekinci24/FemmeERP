using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Tests.Unit;

public class LinqExtensionsBehaviorTests
{
    private static object InvokeSafeToDictionary<TSource, TKey, TValue>(IEnumerable<TSource> src,
        Func<TSource, TKey> keySel, Func<TSource, TValue> valSel, IEqualityComparer<TKey>? cmp = null) where TKey : notnull
    {
        var infraAsm = typeof(global::InventoryERP.Infrastructure.DependencyInjection).Assembly;
        var extType = infraAsm.GetType("Infrastructure.Utils.LinqExtensions", throwOnError: true)!;
        var mi = extType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .First(m => m.Name == "SafeToDictionary" && m.GetGenericArguments().Length == 3);
        var g = mi.MakeGenericMethod(typeof(TSource), typeof(TKey), typeof(TValue));
        return g.Invoke(null, new object?[] { src, keySel, valSel, cmp! })!;
    }

    [Fact]
    public void SafeToDictionary_Last_Value_Wins_On_Duplicate_Key()
    {
        var data = new[] { (K:1, V:"A"), (K:1, V:"B") };
        Func<(int K,string V), int> k = t => t.K; Func<(int K,string V), string> v = t => t.V;
        var dict = (Dictionary<int, string>)InvokeSafeToDictionary(data, k, v);
        dict[1].Should().Be("B");
    }

    [Fact]
    public void SafeToDictionary_Comparer_Applies_To_Key_Eq()
    {
        var data = new[] { (K:"sku", V:1), (K:"SKU", V:2) };
        Func<(string K,int V), string> k = t => t.K; Func<(string K,int V), int> v = t => t.V;
        var cmp = StringComparer.OrdinalIgnoreCase;
        var dict = (Dictionary<string, int>)InvokeSafeToDictionary(data, k, v, cmp);
        dict.Count.Should().Be(1);
        dict["SKU"].Should().Be(2);
    }

    [Fact]
    public void SafeToDictionary_Empty_Source_Produces_Empty_Dictionary()
    {
        var data = Array.Empty<(int K, string V)>();
        Func<(int K,string V), int> k = t => t.K; Func<(int K,string V), string> v = t => t.V;
        var dict = (Dictionary<int, string>)InvokeSafeToDictionary(data, k, v);
        dict.Should().BeEmpty();
    }
}

