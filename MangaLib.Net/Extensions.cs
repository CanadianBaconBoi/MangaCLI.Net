#region header
// MangaCLI.Net : A Featureful Manga Downloader
// Copyright (C)  2024 canadian
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using MangaLib.Net.Base.Metadata;
using MangaLib.Net.Base.Models;
using MangaLib.Net.Connectors.Metadata;

namespace MangaLib.Net;

internal static class UriExtensions
{
    internal static Uri Combine(this Uri self, string other) => new(self, other);

    internal static Uri Combine(this Uri self, Uri other) => new(self, other);

    internal static Uri CombineRaw(this Uri self, string other) => new($"{self}{other}");

    internal static Uri CombineRaw(this Uri self, Uri other) => new($"{self}{other}");
}

internal static class EnumDescriptionExtension
{
    public static string GetDescription<T>(this T enumerationValue,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        Type enumerationType)
        where T : Enum
    {
        if (enumerationValue.GetType() != enumerationType)
            throw new TypeAccessException("Type passed to GetDescription is not equal to type of value passed");
        return enumerationType
            .GetField(enumerationValue.ToString())
            ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() is DescriptionAttribute attribute
            ? attribute.Description
            : enumerationValue.ToString();
    }

    public static string GetMylarDescription<T>(this T enumerationValue,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        Type enumerationType)
        where T : Enum
    {
        if (enumerationValue.GetType() != enumerationType)
            throw new TypeAccessException("Type passed to GetMylarDescription is not equal to type of value passed");
        return enumerationType
            .GetField(enumerationValue.ToString())
            ?.GetCustomAttributes(typeof(MylarDescriptionAttribute), false)
            .FirstOrDefault() is MylarDescriptionAttribute attribute
            ? attribute.Description
            : enumerationValue.ToString();
    }
}

internal static class EnumerableExtensions
{
    public static IEnumerable<T> NullToEmpty<T>(this IEnumerable<T>? src) => src ?? [];
    public static T[]? EmptyToNull<T>(this T[] src) => src.Length == 0 ? null : src;
}

internal static class StringArrayExtensions
{
    private static Dictionary<string, int> ToSortDictionary(this List<String> priorityOverrides, List<String> mainPriorities)
    {
        var dict = priorityOverrides
            .Concat(mainPriorities.Where(provider => provider is not "connector"))
            .Select((name, index) => (name, int.MaxValue - index)).ToDictionary();
        dict.TryAdd("connector", 0);
        return dict;
    }
    
    private static IComparer<string> ToComparer(this Dictionary<string, int> sortDict)
        => Comparer<string>.Create((a, b) => sortDict.GetValueOrDefaultLazy(b, () => MetadataProviderWrapper.MetadataProviders[b].DefaultPriority) - sortDict.GetValueOrDefaultLazy(a, () => MetadataProviderWrapper.MetadataProviders[a].DefaultPriority));

    private static TValue GetValueOrDefaultLazy<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> defaultGetter)
        where TKey : notnull => dict.TryGetValue(key, out TValue? value) ? value : defaultGetter.Invoke();
    
    private static IEnumerable<ComicInfo> OrderByOverrides(this IEnumerable<(string Provider, ComicInfo Info)> input, List<String> mainPriorities, List<String> overrides) =>
        input.OrderBy(a => a.Provider, overrides.ToSortDictionary(mainPriorities).ToComparer()).Select(a => a.Info);

    public static ComicInfo? FirstByOverridesOrNull(
        this IEnumerable<(string Provider, ComicInfo Info)> source, List<String> mainPriorities, List<String> overrides, Func<ComicInfo, bool>? predicate = null
    ) => predicate is { } ? source.OrderByOverrides(mainPriorities, overrides).FirstOrDefault(predicate) : source.OrderByOverrides(mainPriorities, overrides).FirstOrDefault();
    
    public static ComicInfo FirstByOverrides(
        this IEnumerable<(string Provider, ComicInfo Info)> source, List<String> mainPriorities, List<String> overrides, Func<ComicInfo, bool>? predicate = null
    ) => predicate is { } ? source.OrderByOverrides(mainPriorities, overrides).First(predicate) : source.OrderByOverrides(mainPriorities, overrides).First();
}
