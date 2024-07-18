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

namespace MangaLib.Net.Models;

// ReSharper disable InconsistentNaming
public class ComicInfoWrapper: Base.Models.ComicInfo
{
    public static ComicInfoWrapper Merge(List<(string Provider, Base.Models.ComicInfo Info)> comics, List<String> mainPriorities, ComicInfoPriorities priorityOverrides)
    {
        var comicInfo = new ComicInfoWrapper()
        {
            Identifier      = comics.FirstByOverrides(mainPriorities, priorityOverrides.Identifier).Identifier,
            Authors         = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Authors,         info => info.Authors?.EmptyToNull()                   is { })?.Authors ?? [],
            Penciller       = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Penciller,       info => info.Penciller?.EmptyToNull()                 is { })?.Penciller ?? [],
            Inker           = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Inker,           info => info.Inker?.EmptyToNull()                     is { })?.Inker ?? [],
            Colorist        = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Colorist,        info => info.Colorist?.EmptyToNull()                  is { })?.Colorist ?? [],
            Letterer        = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Letterer,        info => info.Letterer?.EmptyToNull()                  is { })?.Letterer ?? [],
            CoverArtist     = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.CoverArtist,     info => info.CoverArtist?.EmptyToNull()               is { })?.CoverArtist ?? [],
            Editors         = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Editors,         info => info.Editors?.EmptyToNull()                   is { })?.Editors ?? [],
            Publishers      = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Publishers,      info => info.Publishers?.EmptyToNull()                is { })?.Publishers ?? [],
            Title           = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Title,           info => info.Title                                    is {Length: >0})?.Title ?? string.Empty,
            Country         = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Country,         info => info.Country                                  is {Length: >0})?.Country ?? "jp",
            Status          = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Status,          info => info.Status                                   is { })?.Status ?? StatusType.Unknown,
            Links           = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Links,           info => info.Links?.ToArray().EmptyToNull()           is { })?.Links ?? [],
            TotalChapters   = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.TotalChapters,   info => info.TotalChapters                            is { })?.TotalChapters ?? 0,
            TotalVolumes    = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.TotalVolumes,    info => info.TotalVolumes                             is { })?.TotalVolumes ?? 0,
            Description     = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Description,     info => info.Description                              is {Length: >0})?.Description ?? string.Empty,
            DescriptionHtml = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.DescriptionHtml, info => info.DescriptionHtml                          is {Length: >0})?.DescriptionHtml ?? string.Empty,
            StartDate       = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.StartDate,       info => info.StartDate                                is { })?.StartDate ?? DateOnly.FromDateTime(DateTime.Now),
            EndDate         = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.EndDate,         info => info.EndDate                                  is { })?.EndDate ?? DateOnly.FromDateTime(DateTime.Now),
            CommunityRating = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.CommunityRating, info => info.CommunityRating                          is { })?.CommunityRating ?? 5,
            AgeRating       = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.AgeRating,       info => info.AgeRating                                is { })?.AgeRating ?? AgeRatingType.RatingPending,
            AlternateTitles = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.AlternateTitles, info => info.AlternateTitles?.ToArray().EmptyToNull() is { })?.AlternateTitles ?? [],
            Genres          = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Genres,          info => info.Genres?.EmptyToNull()                    is { })?.Genres ?? [],
            Tags            = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Tags,            info => info.Tags?.EmptyToNull()                      is { })?.Tags ?? [],
            Covers          = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Covers,          info => info.Covers?.ToArray().EmptyToNull()          is { })?.Covers ?? [],
            Review          = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Review,          info => info.Review                                   is {Length: >0})?.Review ?? string.Empty,
            Characters      = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Characters,      info => info.Characters?.EmptyToNull()                is { })?.Characters ?? [],
        };
        
        if(comicInfo is { Penciller: [], Inker: [], Colorist: [], Letterer: [] })
            comicInfo.Artists = comics.FirstByOverridesOrNull(mainPriorities, priorityOverrides.Artists, info => info.Artists?.EmptyToNull() is { })?.Artists ?? [];
        return comicInfo;
    }
}