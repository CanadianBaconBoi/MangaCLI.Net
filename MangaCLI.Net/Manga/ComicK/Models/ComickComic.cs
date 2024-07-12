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

namespace MangaCLI.Net.Manga.ComicK.Models;

#pragma warning disable CS8618
public class ComickComic: IComic
{
    [JsonPropertyName("title")]
    public string Title { get; init; }
    
    [JsonPropertyName("hid")]
    public string Identifier { get; init; }
    
    [JsonPropertyName("slug")]
    public string Slug { get; init; }

    [JsonPropertyName("desc")]
    public string? Description { get; init; }
    
    [JsonPropertyName("cover_url")]
    public string? CoverThumbnail { get; init; }
    
    [JsonIgnore]
    public string? CoverUrl { get; init; }

    [JsonIgnore]
    private ComickComicInfo? _comicInfoRaw;

    [JsonIgnore]
    private ComickComicInfo RawComicInfo =>
        _comicInfoRaw ??= MangaCli.Connector.
            GetClient().GetFromJsonAsync(
            ComickConnector.BaseApiUrl.Combine($"/comic/{Slug}?tachiyomi=true"),
            ComickJsonContext.Default.ComickComicInfo
        ).GetAwaiter().GetResult()!;

    [JsonIgnore]
    private ComicInfo? _comicInfo;

    [JsonIgnore]
    public ComicInfo ComicInfo => _comicInfo ??= new ComicInfo()
    {
        Identifier = int.Parse(RawComicInfo.Comic.Links.FirstOrDefault(link => link.Key == "al").Value ?? "0"),
        
        Authors = RawComicInfo.Authors.Select(author => author.Name).ToArray(),
        Artists = RawComicInfo.Artists.Select(artist => artist.Name).ToArray(),
        Publishers =
            RawComicInfo.Comic.ExtraComicInfo?.Publishers.Select(publisher => publisher.Publisher.Name).ToArray() ?? [],

        Title = RawComicInfo.Comic.Title,
        Country = RawComicInfo.Comic.Country,
        Status = RawComicInfo.Comic.Status switch
        {
            2 => ComicInfo.StatusType.Ended,
            1 => ComicInfo.StatusType.Continuing,
            _ => ComicInfo.StatusType.Unknown
        },
        Links = RawComicInfo.Comic.Links,
        TotalChapters = RawComicInfo.Comic.TotalChapters ?? 0,
        TotalVolumes = (int)MathF.Floor(float.Parse(RawComicInfo.Comic.FinalVolume ?? "0")),
        Description = RawComicInfo.Comic.Description ?? "",
        DescriptionHtml = RawComicInfo.Comic.ParsedDecsription ?? "",
        Year = RawComicInfo.Comic.Year ?? DateTime.Now.Year,
        Month = DateTime.Now.Month, //TODO: Implement metadata lookup with Anilist
        Day = DateTime.Now.Day, //TODO: Implement metadata lookup with Anilist
        CommunityRating = float.Parse(RawComicInfo.Comic.Rating),
        AgeRating = RawComicInfo.Comic.ContentRating switch
        {
            ComickComicInfo.ComicInfo.ComickContentRating.Safe => ComicInfo.AgeRatingType.Everyone,
            ComickComicInfo.ComicInfo.ComickContentRating.Suggestive => ComicInfo.AgeRatingType.Teen,
            ComickComicInfo.ComicInfo.ComickContentRating.Erotica => ComicInfo.AgeRatingType.X18,
            _ => ComicInfo.AgeRatingType.Unknown
        },
        AlternateTitles = RawComicInfo.Comic.Titles.DistinctBy(title => title.Language).Select(title => (title.Language, title.Title)).ToDictionary(),
        Genres = RawComicInfo.Comic.Genres.Where(genre => genre.Genre.Group is "Genre" or "Theme").Select(genre => genre.Genre.Name).ToArray(),
        Tags = RawComicInfo.Comic.Genres.Where(genre => genre.Genre.Group is "Format").Select(genre => genre.Genre.Name).ToArray(),
        Categories = RawComicInfo.Comic.ExtraComicInfo?.ComicCategories.Where(category => category.Upvotes > category.Downvotes).Select(category => category.ComicCategory.Name).ToArray() ?? [],
        Covers = RawComicInfo.Comic.Covers.DistinctBy(cover => cover.Volume).Select(cover => (cover.Volume ?? "1", new ComicInfo.ImageType()
        {
            Width = cover.Width,
            Height = cover.Height,
            Location = new Uri(ComickConnector.BaseImageUrl, cover.ImageKey)
        })).ToDictionary()
    };
    
    
    IEnumerable<IChapter> IComic.GetChapters(string language) => GetChapters(language);

    public IEnumerable<ComickChapter> GetChapters(string language)
    {
        var chaptersUrl = ComickConnector.BaseApiUrl.Combine($"/comic/{Identifier}/chapters?lang={language}&chap-order=1&limit=50");
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
#pragma warning restore CS8618