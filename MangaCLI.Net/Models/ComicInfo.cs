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
using System.ComponentModel.DataAnnotations;
using MangaCLI.Net.Connectors.Metadata;
using MangaCLI.Net.Metadata;

namespace MangaCLI.Net.Models;

// ReSharper disable InconsistentNaming
public class ComicInfo
{
    public long Identifier;

    public string[]? Authors;
    
    public string[]? Artists
    {
        get =>
            Penciller.NullToEmpty()
                .Concat(Inker.NullToEmpty())
                .Concat(Colorist.NullToEmpty())
                .Concat(Letterer.NullToEmpty())
                .Concat(CoverArtist.NullToEmpty())
                .Concat(Editors.NullToEmpty()).ToArray();
        set => Penciller = value;
    }
    
    public string[]? Penciller;
    public string[]? Inker;
    public string[]? Colorist;
    public string[]? Letterer;
    public string[]? CoverArtist;
    public string[]? Editors;
    
    public string[]? Publishers;

    public string? Title;
    public string? Country;
    public StatusType? Status;
    public Dictionary<string, string>? Links;
    public float? TotalChapters;
    public int? TotalVolumes;
    public string? Description;
    public string? DescriptionHtml;
    public DateOnly? StartDate;
    public DateOnly? EndDate;

    [Range(0, 5)]
    public float? CommunityRating;
    public AgeRatingType? AgeRating;
    public Dictionary<string, string>? AlternateTitles;
    public string[]? Genres;
    public string[]? Tags;
    public IEnumerable<(string, ImageType)>? Covers;

    public string? Review;

    public string[]? Characters;

    public static ComicInfo Merge(List<(string Provider, ComicInfo Info)> comics, Config config)
    {
        var comicInfo = new ComicInfo()
        {
            Identifier      = comics.FirstByOverrides(config.PriorityOverrides.Identifier).Identifier,
            Authors         = comics.FirstByOverridesOrNull(config.PriorityOverrides.Authors,         info => info.Authors?.EmptyToNull()                   is { })?.Authors ?? [],
            Penciller       = comics.FirstByOverridesOrNull(config.PriorityOverrides.Penciller,       info => info.Penciller?.EmptyToNull()                 is { })?.Penciller ?? [],
            Inker           = comics.FirstByOverridesOrNull(config.PriorityOverrides.Inker,           info => info.Inker?.EmptyToNull()                     is { })?.Inker ?? [],
            Colorist        = comics.FirstByOverridesOrNull(config.PriorityOverrides.Colorist,        info => info.Colorist?.EmptyToNull()                  is { })?.Colorist ?? [],
            Letterer        = comics.FirstByOverridesOrNull(config.PriorityOverrides.Letterer,        info => info.Letterer?.EmptyToNull()                  is { })?.Letterer ?? [],
            CoverArtist     = comics.FirstByOverridesOrNull(config.PriorityOverrides.CoverArtist,     info => info.CoverArtist?.EmptyToNull()               is { })?.CoverArtist ?? [],
            Editors         = comics.FirstByOverridesOrNull(config.PriorityOverrides.Editors,         info => info.Editors?.EmptyToNull()                   is { })?.Editors ?? [],
            Publishers      = comics.FirstByOverridesOrNull(config.PriorityOverrides.Publishers,      info => info.Publishers?.EmptyToNull()                is { })?.Publishers ?? [],
            Title           = comics.FirstByOverridesOrNull(config.PriorityOverrides.Title,           info => info.Title                                    is {Length: >0})?.Title ?? string.Empty,
            Country         = comics.FirstByOverridesOrNull(config.PriorityOverrides.Country,         info => info.Country                                  is {Length: >0})?.Country ?? "jp",
            Status          = comics.FirstByOverridesOrNull(config.PriorityOverrides.Status,          info => info.Status                                   is { })?.Status ?? StatusType.Unknown,
            Links           = comics.FirstByOverridesOrNull(config.PriorityOverrides.Links,           info => info.Links?.ToArray().EmptyToNull()           is { })?.Links ?? [],
            TotalChapters   = comics.FirstByOverridesOrNull(config.PriorityOverrides.TotalChapters,   info => info.TotalChapters                            is { })?.TotalChapters ?? 0,
            TotalVolumes    = comics.FirstByOverridesOrNull(config.PriorityOverrides.TotalVolumes,    info => info.TotalVolumes                             is { })?.TotalVolumes ?? 0,
            Description     = comics.FirstByOverridesOrNull(config.PriorityOverrides.Description,     info => info.Description                              is {Length: >0})?.Description ?? string.Empty,
            DescriptionHtml = comics.FirstByOverridesOrNull(config.PriorityOverrides.DescriptionHtml, info => info.DescriptionHtml                          is {Length: >0})?.DescriptionHtml ?? string.Empty,
            StartDate       = comics.FirstByOverridesOrNull(config.PriorityOverrides.StartDate,       info => info.StartDate                                is { })?.StartDate ?? DateOnly.FromDateTime(DateTime.Now),
            EndDate         = comics.FirstByOverridesOrNull(config.PriorityOverrides.EndDate,         info => info.EndDate                                  is { })?.EndDate ?? DateOnly.FromDateTime(DateTime.Now),
            CommunityRating = comics.FirstByOverridesOrNull(config.PriorityOverrides.CommunityRating, info => info.CommunityRating                          is { })?.CommunityRating ?? 5,
            AgeRating       = comics.FirstByOverridesOrNull(config.PriorityOverrides.AgeRating,       info => info.AgeRating                                is { })?.AgeRating ?? AgeRatingType.RatingPending,
            AlternateTitles = comics.FirstByOverridesOrNull(config.PriorityOverrides.AlternateTitles, info => info.AlternateTitles?.ToArray().EmptyToNull() is { })?.AlternateTitles ?? [],
            Genres          = comics.FirstByOverridesOrNull(config.PriorityOverrides.Genres,          info => info.Genres?.EmptyToNull()                    is { })?.Genres ?? [],
            Tags            = comics.FirstByOverridesOrNull(config.PriorityOverrides.Tags,            info => info.Tags?.EmptyToNull()                      is { })?.Tags ?? [],
            Covers          = comics.FirstByOverridesOrNull(config.PriorityOverrides.Covers,          info => info.Covers?.ToArray().EmptyToNull()          is { })?.Covers ?? [],
            Review          = comics.FirstByOverridesOrNull(config.PriorityOverrides.Review,          info => info.Review                                   is {Length: >0})?.Review ?? string.Empty,
            Characters      = comics.FirstByOverridesOrNull(config.PriorityOverrides.Characters,      info => info.Characters?.EmptyToNull()                is { })?.Characters ?? [],
        };
        
        if(comicInfo is { Penciller: [], Inker: [], Colorist: [], Letterer: [] })
            comicInfo.Artists = comics.FirstByOverridesOrNull(config.PriorityOverrides.Artists, info => info.Artists?.EmptyToNull() is { })?.Artists ?? [];
        return comicInfo;
    }
    
    public enum AgeRatingType
    {
        [MylarDescription("All")] [Description("Unknown")]
        Unknown,

        [MylarDescription("Adult")] [Description("Adults Only 18+")]
        AdultsOnly18,

        [MylarDescription("All")] [Description("Early Childhood")]
        EarlyChildhood,

        [MylarDescription("All")] [Description("Everyone")]
        Everyone,

        [MylarDescription("9+")] [Description("Everyone 10+")]
        Everyone10,

        [MylarDescription("All")] [Description("G")]
        G,

        [MylarDescription("All")] [Description("Kids to Adults")]
        KidsToAdults,

        [MylarDescription("Adult")] [Description("M")]
        M,

        [MylarDescription("15+")] [Description("MA15+")]
        MA15,

        [MylarDescription("17+")] [Description("Mature 17+")]
        Mature17,

        [MylarDescription("12+")] [Description("PG")]
        PG,

        [MylarDescription("Adult")] [Description("R18+")]
        R18,

        [MylarDescription("All")] [Description("Rating Pending")]
        RatingPending,

        [MylarDescription("15+")] [Description("Teen")]
        Teen,

        [MylarDescription("Adult")] [Description("X18+")]
        X18
    }

    public enum StatusType
    {
        [MylarDescription("Ended")] [Description("Ended")]
        Ended,

        [MylarDescription("Continuing")] [Description("Continuing")]
        Continuing,

        [MylarDescription("Continuing")] [Description("Unknown")]
        Unknown
    }

    public struct ImageType
    {
        public int Width;
        public int Height;
        public Uri Location;
    }
}

internal static class StringArrayExtensions
{
    private static Dictionary<string, int> ToSortDictionary(this List<String> array)
    {
        var dict = array
            .Concat(MangaCli.Config.MainPriorities.Where(provider => provider is not "connector"))
            .Select((name, index) => (name, int.MaxValue - index)).ToDictionary();
        dict.TryAdd("connector", 0);
        return dict;
    }
    
    private static IComparer<string> ToComparer(this Dictionary<string, int> sortDict)
        => Comparer<string>.Create((a, b) => sortDict.GetValueOrDefaultLazy(b, () => IMetadataProvider.MetadataProviders[b].DefaultPriority) - sortDict.GetValueOrDefaultLazy(a, () => IMetadataProvider.MetadataProviders[a].DefaultPriority));

    private static TValue GetValueOrDefaultLazy<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> defaultGetter)
        where TKey : notnull => dict.TryGetValue(key, out TValue? value) ? value : defaultGetter.Invoke();
    
    private static IEnumerable<ComicInfo> OrderByOverrides(this IEnumerable<(string Provider, ComicInfo Info)> input, List<String> overrides) =>
        input.OrderBy(a => a.Provider, overrides.ToSortDictionary().ToComparer()).Select(a => a.Info);

    public static ComicInfo? FirstByOverridesOrNull(
        this IEnumerable<(string Provider, ComicInfo Info)> source, List<String> overrides, Func<ComicInfo, bool>? predicate = null
    ) => predicate is { } ? source.OrderByOverrides(overrides).FirstOrDefault(predicate) : source.OrderByOverrides(overrides).FirstOrDefault();
    
    public static ComicInfo FirstByOverrides(
        this IEnumerable<(string Provider, ComicInfo Info)> source, List<String> overrides, Func<ComicInfo, bool>? predicate = null
    ) => predicate is { } ? source.OrderByOverrides(overrides).First(predicate) : source.OrderByOverrides(overrides).First();
}
