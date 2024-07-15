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
using AniListNet;
using AniListNet.Objects;
using Image = SixLabors.ImageSharp.Image;

namespace MangaCLI.Net.Manga.ComicK.Models;

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

    [JsonIgnore] private ComickComicInfo? _comicInfoRaw;

    [JsonIgnore]
    private ComickComicInfo RawComicInfo =>
        _comicInfoRaw ??= MangaCli.Connector.GetClient().GetFromJsonAsync(
            ComickConnector.BaseApiUrl.Combine($"/comic/{Slug}?tachiyomi=true"),
            ComickJsonContext.Default.ComickComicInfo
        ).GetAwaiter().GetResult()!;

    [JsonIgnore] private ComicInfo? _comicInfo;

    [JsonIgnore]
    public ComicInfo ComicInfo => _comicInfo ??= new ComicInfo()
    {
        Identifier = int.Parse(RawComicInfo.Comic.Links.FirstOrDefault(link => link.Key == "al").Value ?? "0"),

        Authors = RawComicInfo.Authors.Select(author => author.Name).ToArray(),
        Artists = RawComicInfo.Artists.Select(artist => artist.Name).ToArray(),
        Publishers = RawComicInfo.Comic.ExtraComicInfo?.Publishers
            .Select(publisher => publisher.Publisher.Name).ToArray() ?? [],

        Title = AnilistInfo?.Title.EnglishTitle ?? RawComicInfo.Comic.Title,
        Country = RawComicInfo.Comic.Country, //TODO: Find AniList Library with this info that doesn't suck
        Status = AnilistInfo is { Status: var status }
            ? status switch
            {
                MediaStatus.Releasing => ComicInfo.StatusType.Continuing,
                MediaStatus.Hiatus => ComicInfo.StatusType.Continuing,
                MediaStatus.Canceled => ComicInfo.StatusType.Ended,
                MediaStatus.Finished => ComicInfo.StatusType.Ended,
                _ => ComicInfo.StatusType.Unknown
            }
            : RawComicInfo.Comic.Status switch
            {
                2 => ComicInfo.StatusType.Ended,
                1 => ComicInfo.StatusType.Continuing,
                _ => ComicInfo.StatusType.Unknown
            },
        Links = RawComicInfo.Comic.Links,
        TotalChapters = AnilistInfo?.Chapters ?? RawComicInfo.Comic.TotalChapters ?? 0,
        TotalVolumes = AnilistInfo?.Volumes ?? (int)MathF.Floor(float.Parse(RawComicInfo.Comic.FinalVolume ?? "0")),
        Description = RawComicInfo.Comic.Description ?? AnilistInfo?.Description ?? string.Empty,
        DescriptionHtml = RawComicInfo.Comic.ParsedDecsription ?? AnilistInfo?.DescriptionHtml ?? string.Empty,
        StartDate = new DateOnly(
            AnilistInfo?.StartDate.Year ?? RawComicInfo.Comic.Year ?? DateTime.Now.Year,
            AnilistInfo?.StartDate.Month ?? 1,
            AnilistInfo?.StartDate.Day ?? 1
        ),
        EndDate = AnilistInfo?.EndDate.Year is { }
            ? new DateOnly(
                AnilistInfo.EndDate.Year.Value,
                AnilistInfo.EndDate.Month ?? 1,
                AnilistInfo.EndDate.Day ?? 1
            )
            : null,
        CommunityRating = float.Parse(RawComicInfo.Comic.Rating),
        AgeRating = RawComicInfo.Comic.ContentRating switch
        {
            ComickComicInfo.ComicInfo.ComickContentRating.Safe => ComicInfo.AgeRatingType.Everyone,
            ComickComicInfo.ComicInfo.ComickContentRating.Suggestive => ComicInfo.AgeRatingType.Teen,
            ComickComicInfo.ComicInfo.ComickContentRating.Erotica => ComicInfo.AgeRatingType.X18,
            _ => ComicInfo.AgeRatingType.Unknown
        },
        AlternateTitles = RawComicInfo.Comic.Titles.Concat(AnilistInfo is { }
                ? [
                    new ComickComicInfo.ComicInfo.ComickTitle
                        { Language = "en", Title = AnilistInfo.Title.EnglishTitle ?? string.Empty },
                    new ComickComicInfo.ComicInfo.ComickTitle
                        { Language = "ja", Title = AnilistInfo.Title.NativeTitle },
                    new ComickComicInfo.ComicInfo.ComickTitle
                        { Language = "ja-ro", Title = AnilistInfo.Title.RomajiTitle }
                ] 
                : []
            )
            .DistinctBy(title => title.Language)
            .Select(title => (title.Language, title.Title)).ToDictionary(),
        Genres = AnilistInfo?.Genres ?? RawComicInfo.Comic.Genres
            .Where(genre => genre.Genre.Group is "Genre" or "Theme")
            .Select(genre => genre.Genre.Name).ToArray(),
        Tags = AnilistInfo?.GetTagsAsync().GetAwaiter().GetResult()
            .Select(tag => tag.Name).ToArray()
               ?? RawComicInfo.Comic.Genres
                   .Where(genre => genre.Genre.Group is "Format")
                   .Select(genre => genre.Genre.Name)
                   .Concat(
                       RawComicInfo.Comic.ExtraComicInfo?.ComicCategories
                        .Where(category => category.Upvotes > category.Downvotes)
                        .Select(category => category.ComicCategory.Name) ?? []
                       ).ToArray(),
        Covers = RawComicInfo.Comic.Covers
            .DistinctBy(cover => cover.Volume)
            .Select(cover => (cover.Volume ?? "1",
                new ComicInfo.ImageType()
                {
                    Width = cover.Width,
                    Height = cover.Height,
                    Location = new Uri(ComickConnector.BaseImageUrl, cover.ImageKey)
                }))
            .Concat(GetAnilistCover(AnilistInfo))
    };

    private IEnumerable<(string, ComicInfo.ImageType)> GetAnilistCover(Media? anilistInfo)
    {
        if (anilistInfo == null || string.IsNullOrEmpty(anilistInfo.Cover.ExtraLargeImageUrl.ToString()))
            yield break;

        using var response = MangaCli.Connector.GetClient()
            .Send(new HttpRequestMessage(HttpMethod.Get, anilistInfo.Cover.ExtraLargeImageUrl));
        using var image = Image.Load(response.Content.ReadAsStream());
        yield return ("0", new ComicInfo.ImageType()
        {
            Width = image.Width,
            Height = image.Height,
            Location = anilistInfo.Cover.ExtraLargeImageUrl
        });
    }

    private Media? _anilistInfo;

    [JsonIgnore]
    public Media AnilistInfo
    {
        get
        {
            try
            {
                return _anilistInfo ??= MangaCli.AnilistClient.GetMediaAsync(
                    int.Parse(RawComicInfo.Comic.Links.FirstOrDefault(link => link.Key == "al").Value ?? "0")
                ).GetAwaiter().GetResult();
            }
            catch
            {
                return _anilistInfo = new Media();
            }
        }
    }

    private AniPagination<StaffEdge>? _anilistStaff;

    public AniPagination<StaffEdge>? AnilistStaff =>
        _anilistStaff ??= AnilistInfo?.GetStaffAsync().GetAwaiter().GetResult();

    private AniPagination<CharacterEdge>? _anilistCharacters;

    public AniPagination<CharacterEdge>? AnilistCharacters =>
        _anilistCharacters ??= AnilistInfo?.GetCharactersAsync().GetAwaiter().GetResult();

    private AniPagination<MediaReview>? _anilistReviews;

    public AniPagination<MediaReview>? AnilistReviews =>
        _anilistReviews ??= AnilistInfo?.GetReviewsAsync().GetAwaiter().GetResult();


    IEnumerable<IChapter> IComic.GetChapters(string language) => GetChapters(language);

    public IEnumerable<ComickChapter> GetChapters(string language)
    {
        var chaptersUrl =
            ComickConnector.BaseApiUrl.Combine($"/comic/{Identifier}/chapters?lang={language}&chap-order=1&limit=50");
        ComickChapters? chapters;
        var x = 0;
        var page = 1;
        do
        {
            chapters = MangaCli.Connector.GetClient().GetFromJsonAsync(
                chaptersUrl.CombineRaw($"&page={page++}"),
                ComickJsonContext.Default.ComickChapters
            ).GetAwaiter().GetResult();

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