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

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AniListNet.Objects;
using MangaCLI.Net.Metadata;
using MangaCLI.Net.Models;

namespace MangaCLI.Net.Connectors.Manga.ComicK.Models;

public class ComickChapter : IChapter
{
#pragma warning disable CS8618
    [JsonPropertyName("title")] public string? Title { get; set; }

    [JsonPropertyName("hid")] public string Identifier { get; init; }

    [JsonPropertyName("chap")] public string? ChapterIndex { get; init; }

    [JsonPropertyName("vol")] public string? VolumeIndex { get; set; }

    [JsonPropertyName("group_name")] public string[]? GroupName { get; set; }

    [JsonPropertyName("lang")] public string Language { get; init; }

    [JsonIgnore] public ComickComic Owner { get; set; }

    [JsonPropertyName("up_count")] public int UpvoteCount { get; init; }
    [JsonPropertyName("down_count")] public int DownvoteCount { get; init; }

    [JsonIgnore] public int PageCount => Pages.Length;


    [JsonIgnore] private IPage[]? _pages;

    [JsonIgnore]
    public IPage[] Pages =>
        // ReSharper disable once CoVariantArrayConversion
        _pages ??= MangaCli.Connector.GetClient().GetFromJsonAsync(
            ComickConnector.BaseApiUrl.Combine($"/chapter/{Identifier}?tachiyomi=true"),
            ComickJsonContext.Default.ComickChapterWrapper).GetAwaiter().GetResult()?.Chapter.Pages ?? [];
#pragma warning restore CS8618

    public MetadataComicRack GetComicRackMetadata()
    {
        MetadataComicRack.ComicPageInfo[] pages = new MetadataComicRack.ComicPageInfo[PageCount];

        for (var i = 0; i < PageCount; i++)
            pages[i] = new MetadataComicRack.ComicPageInfo()
            {
                Image = i + 1,
                Type = MetadataComicRack.ComicPageType.Story,
                DoublePage = Pages[i].Width > Pages[i].Height,
                ImageSize = 0,
                ImageWidth = Pages[i].Width,
                ImageHeight = Pages[i].Height
            };

        const StringComparison strCmp = StringComparison.InvariantCultureIgnoreCase;

        if (!int.TryParse(VolumeIndex, out var volumeIndex))
            volumeIndex = -1;

        return new MetadataComicRack
        {
            Title = Title ?? "",
            Series = Owner.ComicInfo.Title,
            Number = ChapterIndex ?? "0",
            Count = (int)MathF.Floor(Owner.ComicInfo.TotalChapters),
            Volume = volumeIndex != -1 ? volumeIndex : null,
            Summary = "", //TODO: Find a metadata provider with per-chapter metadata
            Notes = "Generated by MangaCLI.Net",
            Year = DateTime.Now.Year, //TODO: Find a metadata provider with per-chapter metadata
            Month = DateTime.Now.Month, //TODO: Find a metadata provider with per-chapter metadata
            Day = DateTime.Now.Day, //TODO: Find a metadata provider with per-chapter metadata
            Writer = string.Join(',',
                Owner.AnilistStaff?.Data
                    .Where(edge =>
                        edge.Role.Contains("write", strCmp) ||
                        edge.Role.Contains("story", strCmp) ||
                        edge.Role.Contains("author", strCmp))
                    .Select(edge => edge.Staff.Name.FullName) ?? Owner.ComicInfo.Authors),
            Penciller = string.Join(',',
                Owner.AnilistStaff?.Data
                    .Where(edge => edge.Role.Contains("pencil", strCmp) || edge.Role.Contains("art", strCmp))
                    .Select(edge => edge.Staff.Name.FullName) ?? Owner.ComicInfo.Artists),
            Inker = string.Join(',',
                Owner.AnilistStaff?.Data
                    .Where(edge => edge.Role.Contains("ink", strCmp) || edge.Role.Contains("black", strCmp))
                    .Select(edge => edge.Staff.Name.FullName) ?? Array.Empty<string>()),
            Colorist = string.Join(',',
                Owner.AnilistStaff?.Data
                    .Where(edge => edge.Role.Contains("color", strCmp) || edge.Role.Contains("colour", strCmp))
                    .Select(edge => edge.Staff.Name.FullName) ?? Array.Empty<string>()),
            Letterer = string.Join(',',
                Owner.AnilistStaff?.Data.Where(edge => edge.Role.Contains("letter", strCmp))
                    .Select(edge => edge.Staff.Name.FullName) ?? Array.Empty<string>()),
            CoverArtist = string.Join(',',
                Owner.AnilistStaff?.Data.Where(edge => edge.Role.Contains("cover", strCmp))
                    .Select(edge => edge.Staff.Name.FullName) ?? Array.Empty<string>()),
            Editor = string.Join(',',
                Owner.AnilistStaff?.Data.Where(edge => edge.Role.Contains("edit", strCmp))
                    .Select(edge => edge.Staff.Name.FullName) ?? Array.Empty<string>()),
            Publisher = string.Join(',', Owner.ComicInfo.Publishers),
            Characters = string.Join(',', Owner.AnilistCharacters?.Data //TODO: Find a metadata provider with per-chapter metadata
                .Where(character => character.Role == CharacterRole.Main)
                .Select(character => character.Character.Name.FullName)
                                          ?? []),
            MainCharacterOrTeam = Owner.AnilistCharacters?.Data //TODO: Find a metadata provider with per-chapter metadata
                .FirstOrDefault(character => character.Role == CharacterRole.Main)?.Character.Name.FullName
                                  ?? string.Empty,
            Teams = "", //TODO: Find a metadata provider with per-chapter metadata
            Locations = "", //TODO: Find a metadata provider with per-chapter metadata
            StoryArc = "", //TODO: Find a metadata provider with per-chapter metadata
            SeriesGroup = "", //TODO: Find a metadata provider with per-chapter metadata
            Genre = string.Join(',', Owner.ComicInfo.Genres),
            Tags = string.Join(',', Owner.ComicInfo.Tags),
            Web = Owner.ComicInfo.Links?
                .OrderBy(link => link.Key, LinkPriorityComparer.Instance)
                .Select(LinkPriorityComparer.ToLink)
                .FirstOrDefault()
                  ?? string.Empty,
            PageCount = PageCount,
            LanguageISO = Language,
            Format = "Digital",
            BlackAndWhite = Owner.ComicInfo.Genres
                .Any(genre => genre.ToLower() is "colored" or "coloured")
                    ? MetadataComicRack.YesNoType.Yes
                    : MetadataComicRack.YesNoType.No,
            Manga = MetadataComicRack.MangaType.YesAndRightToLeft,
            ScanInformation = $"Translated by: {GroupName?.FirstOrDefault() ?? "UNKNOWN"}",
            AgeRating = Owner.ComicInfo.AgeRating,
            CommunityRating = UpvoteCount + DownvoteCount != 0
                ? MathF.Round(UpvoteCount / ((float)UpvoteCount + DownvoteCount) * 5f, 2)
                : 0f,
            Review = Owner.AnilistReviews?.Data.FirstOrDefault()?.Summary ?? "", //TODO: Find a metadata provider with per-chapter metadata
            Pages = pages
        };
    }

    private class LinkPriorityComparer : IComparer<string>
    {
        public static readonly LinkPriorityComparer Instance = new();

        public int Compare(string? x, string? y) =>
            (x is { } ? Priority(x) : int.MaxValue) - (y is { } ? Priority(y) : int.MaxValue);

        private int Priority(string id)
        {
            return id switch
            {
                "al" => 0,
                "mal" => 1,
                "mu" => 2,
                "ap" => 3,
                "bw" => 4,
                "amz" => 5,
                "raw" => 6,
                "engtl" => 7,
                "ebj" => 8,
                _ => int.MaxValue
            };
        }

        public static string ToLink(KeyValuePair<string, string> link)
        {
            return link.Key switch
            {
                "al" => $"https://anilist.co/manga/{link.Value}",
                "mal" => $"https://myanimelist.net/manga/{link.Value}",
                "mu" => $"https://www.mangaupdates.com/series.html?id={link.Value}",
                "ap" => $"https://www.anime-planet.com/manga/{link.Value}",
                "bw" => $"https://bookwalker.jp/{link.Value}",
                _ => link.Value
            };
        }
    }
}