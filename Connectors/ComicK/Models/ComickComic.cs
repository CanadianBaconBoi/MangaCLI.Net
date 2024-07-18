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
using MangaLib.Net.Base.Helpers;
using MangaLib.Net.Base.Models;
using MangaLib.Net.Connectors.Manga.ComicK.Models;

namespace Connectors.ComicK.Models;

public class ComickComic : IComic
{
#pragma warning disable CS8618

    [JsonPropertyName("title")] public string Title { get; init; }
    [JsonPropertyName("hid")] public string Identifier { get; init; }
    [JsonPropertyName("slug")] public string Slug { get; init; }
    [JsonPropertyName("desc")] public string? Description { get; init; }
    [JsonPropertyName("cover_url")] public string? CoverThumbnail { get; init; }
    [JsonIgnore] public string? CoverUrl { get; init; }
#pragma warning restore CS8618

    [JsonIgnore]
    private ComickComicInfo? _comicInfoRaw;

    [JsonIgnore]
    private ComickComicInfo ComickComicInfo =>
        _comicInfoRaw ??= MangaLib.Net.Base.MangaLib.GetClient().GetFromJsonAsync(
            ComickConnector.BaseApiUrl.Combine($"/comic/{Slug}?tachiyomi=true"),
            ComickJsonContext.Default.ComickComicInfo
        ).GetAwaiter().GetResult()!;

    #pragma warning disable CS8509
    public Dictionary<string, string>? GetMetadataIdentifiers() => ComickComicInfo.Comic.Links?
        .Where(link => link.Key is "al" or "mal" or "mu" or "ap" or "bw")
        .Select(link => link.Key switch
            {
                "al" => ("anilist", link.Value),
                "mal" => ("myanimelist", link.Value),
                "mu" => ("mangaupdates", link.Value),
                "ap" => ("animeplanet", link.Value),
                "bw" => ("bookwalker", link.Value)
            }
        ).ToDictionary();
    #pragma warning restore CS8509

    [JsonIgnore]
    private ComicInfo? _rawComicInfo;

    [JsonIgnore]
    public ComicInfo RawComicInfo => _rawComicInfo ??= new ComicInfo()
    {
        Identifier = ComickComicInfo.Comic.Identifier,

        Authors = ComickComicInfo.Authors?.Select(author => author.Name).ToArray().EmptyToNull(),
        Artists = ComickComicInfo.Artists?.Select(artist => artist.Name).ToArray().EmptyToNull(),
        Publishers = ComickComicInfo.Comic.ExtraComicInfo?.Publishers?
            .Select(publisher => publisher.Publisher.Name).ToArray().EmptyToNull(),

        Title = ComickComicInfo.Comic.Title,
        Country = ComickComicInfo.Comic.Country,
        Status = ComickComicInfo.Comic.Status switch
        {
            2 => ComicInfo.StatusType.Ended,
            1 => ComicInfo.StatusType.Continuing,
            null => null,
            _ => ComicInfo.StatusType.Unknown
        },
        Links = ComickComicInfo.Comic.Links,
        TotalChapters = ComickComicInfo.Comic.TotalChapters,
        TotalVolumes = ComickComicInfo.Comic.FinalVolume is { } ? (int)MathF.Floor(float.Parse(ComickComicInfo.Comic.FinalVolume)) : null,
        Description = ComickComicInfo.Comic.Description,
        DescriptionHtml = ComickComicInfo.Comic.ParsedDecsription,
        StartDate = ComickComicInfo.Comic.Year is { } ? new DateOnly(
            ComickComicInfo.Comic.Year.Value,
            1,
            1
        ) : null,
        EndDate = null,
        CommunityRating = ComickComicInfo.Comic.Rating is { } ? float.Parse(ComickComicInfo.Comic.Rating) : null,
        AgeRating = ComickComicInfo.Comic.ContentRating switch
        {
            ComickComicInfo.ComicInfo.ComickContentRating.Safe => ComicInfo.AgeRatingType.Everyone,
            ComickComicInfo.ComicInfo.ComickContentRating.Suggestive => ComicInfo.AgeRatingType.Teen,
            ComickComicInfo.ComicInfo.ComickContentRating.Erotica => ComicInfo.AgeRatingType.X18,
            null => null,
            _ => ComicInfo.AgeRatingType.Unknown
        },
        AlternateTitles = ComickComicInfo.Comic.Titles?
            .Select(title => (title.Language, title.Title))
            .DistinctBy(title => title.Language).ToDictionary() is { } titleDict
            ? (titleDict.Count == 0 ? null : titleDict)
            : null,
        Genres = ComickComicInfo.Comic.Genres?
            .Where(genre => genre.Genre.Group is "Genre" or "Theme")
            .Select(genre => genre.Genre.Name).ToArray().EmptyToNull(),
        Tags = ComickComicInfo.Comic.Genres?
                   .Where(genre => genre.Genre.Group is "Format")
                   .Select(genre => genre.Genre.Name)
                   .Concat(
                       ComickComicInfo.Comic.ExtraComicInfo?.ComicCategories?
                           .Where(category => category.Upvotes > category.Downvotes)
                           .Select(category => category.ComicCategory.Name) ?? []).ToArray().EmptyToNull()
               ?? ComickComicInfo.Comic.ExtraComicInfo?.ComicCategories?
                   .Where(category => category.Upvotes > category.Downvotes)
                   .Select(category => category.ComicCategory.Name).ToArray().EmptyToNull(),
        Covers = ComickComicInfo.Comic.Covers?
            .DistinctBy(cover => cover.Volume)
            .Select(cover => (cover.Volume ?? "1",
                new ComicInfo.ImageType()
                {
                    Width = cover.Width,
                    Height = cover.Height,
                    Location = new Uri(ComickConnector.BaseImageUrl, cover.ImageKey)
                })).ToArray().EmptyToNull(),
        
        Review = null,
        Characters = null
    };
    
    

    public async IAsyncEnumerable<IChapter> GetChapters(string language)
    {
        var chaptersUrl = ComickConnector.BaseApiUrl.Combine($"/comic/{Identifier}/chapters?lang={language}&chap-order=1&limit=50");
        ComickChapters? chapters;
        var x = 0;
        var page = 1;
        do
        {
            chapters = await MangaLib.Net.Base.MangaLib.GetClient().GetFromJsonAsync(
                chaptersUrl.CombineRaw($"&page={page++}"),
                ComickJsonContext.Default.ComickChapters
            );

            if (chapters == null) break;
            foreach (var chapter in chapters.Chapters)
            {
                chapter.Owner = this;
                yield return chapter;
            }

            x += chapters.Chapters.Length;
        } while (x < chapters.Total);
    }
}