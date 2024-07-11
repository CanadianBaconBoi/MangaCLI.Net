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

public class ComickChapter: IChapter
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("hid")]
    public string Identifier { get; init; }
    
    [JsonPropertyName("chap")]
    public string? ChapterIndex { get; init; }

    [JsonPropertyName("group_name")]
    public string[]? GroupName { get; set; }
    
    [JsonPropertyName("lang")]
    public string Language { get; init; }
    
    [JsonIgnore]
    public ComickComic Owner { get; set; }
    
    [JsonPropertyName("up_count")]
    public int UpvoteCount { get; init; }
    [JsonPropertyName("down_count")]
    public int DownvoteCount { get; init; }

    [JsonIgnore] public int PageCount => Pages?.Length ?? 0;
    
    [JsonIgnore]
    private IPage[]? Pages { get; set; }

    IPage[] IChapter.GetPages() => GetPages();

    private IPage[] GetPages()
    {
        // ReSharper disable once CoVariantArrayConversion
        return Pages ??= MangaCli.Connector.GetClient().GetFromJsonAsync<ComickChapterWrapper>(
            ComickConnector.BaseApiUrl.Combine($"/chapter/{Identifier}?tachiyomi=true"),
            ComickJsonContext.Default.Options).GetAwaiter().GetResult()?.Chapter.Pages ?? [];
    }
    
    public ComicInfo GetComicInfo()
    {
        if (Pages == null)
            GetPages();

        ComicInfo.ComicPageInfo[] pages = new ComicInfo.ComicPageInfo[PageCount];
        for (int i = 0; i < PageCount; i++)
        {
            pages[i] = new ComicInfo.ComicPageInfo()
            {
                Image = i + 1,
                Type = ComicInfo.ComicPageType.Story,
                DoublePage = Pages![i].Width > Pages[i].Height,
                ImageSize = MangaCli.Connector.GetClient().Send(new HttpRequestMessage(HttpMethod.Head, Pages[i].Url))
                    .Content.Headers.ContentLength.GetValueOrDefault(0),
                ImageWidth = Pages[i].Width,
                ImageHeight = Pages[i].Height
            };
        }
        
        return new ComicInfo
        {
            Identifier = int.Parse(Owner.ComicInfo.Comic.Links.FirstOrDefault(link => link.Key == "al").Value ?? "0"),
            CoverUrl = Owner.CoverThumbnail,
            Status = Owner.ComicInfo.Comic.Status switch
            {
                2 => ComicInfo.StatusType.Ended,
                1 => ComicInfo.StatusType.Continuing,
                _ => ComicInfo.StatusType.Unknown
            },
            Title = Title ?? "",
            Series = Owner.Title,
            Number = ChapterIndex ?? "0",
            Count = (int)MathF.Floor(Owner.ComicInfo.Comic.TotalChapters ?? 0F),
            Notes = "Generated by MangaCLI.Net",
            Year = Owner.ComicInfo.Comic.Year,
            Writer = String.Join(',', Owner.ComicInfo.Authors.Select(author => author.Name)),
            Penciller = string.Join(',', Owner.ComicInfo.Artists.Select(artist => artist.Name)),
            Publisher = string.Join(',', Owner.ComicInfo.Comic.ExtraComicInfo?.Publishers.Select(publisher => publisher.Publisher.Name) ?? Owner.ComicInfo.Authors.Select(author => author.Name)),
            Genre = string.Join(',', Owner.ComicInfo.Comic.Genres.Where(genre => genre.Genre.Group is "Genre" or "Theme").Select(genre => genre.Genre.Name)),
            Tags = string.Join(',', Owner.ComicInfo.Comic.Genres.Where(genre => genre.Genre.Group is "Format").Select(genre => genre.Genre.Name)
                .Concat(Owner.ComicInfo.Comic.ExtraComicInfo?.ComicCategories.Where(category => category.Upvotes > category.Downvotes).Select(category => category.ComicCategory.Name) ?? Array.Empty<string>())),
            Web = Owner.ComicInfo.Comic.Links.Select(link =>
                    {
                        switch (link.Key)
                        {
                            case "al":
                                return $"https://anilist.co/manga/{link.Value}";
                            case "mal":
                                return $"https://myanimelist.net/manga/{link.Value}";
                            case "mu":
                                return $"https://www.mangaupdates.com/series.html?id={link.Value}";
                            case "ap":
                                return $"https://www.anime-planet.com/manga/{link.Value}";
                            case "bw":
                                return $"https://bookwalker.jp/{link.Value}";
                            case "amz": case "raw": case "engtl": case "ebj":
                                return link.Value;
                        }
                        return string.Empty;
                    }).First()
            ,
            PageCount = this.PageCount,
            LanguageISO = this.Language,
            Format = "Digital",
            BlackAndWhite = Owner.ComicInfo.Comic.Genres.Exists(genre => genre.Genre.Name.Contains("Colored") || genre.Genre.Name.Contains("Coloured")) ? ComicInfo.YesNoType.No : ComicInfo.YesNoType.Yes,
            Manga = ComicInfo.MangaType.YesAndRightToLeft,
            ScanInformation = $"Translated by: {GroupName?.First() ?? "UNKNOWN"}",
            AgeRating = Owner.ComicInfo.Comic.ContentRating switch
            {
                ComickComicInfo.ComicInfo.ComickContentRating.Safe => ComicInfo.AgeRatingType.Everyone,
                ComickComicInfo.ComicInfo.ComickContentRating.Suggestive => ComicInfo.AgeRatingType.Teen,
                ComickComicInfo.ComicInfo.ComickContentRating.Erotica => ComicInfo.AgeRatingType.X18,
                _ => ComicInfo.AgeRatingType.M
            },
            CommunityRating =
                this.UpvoteCount + this.DownvoteCount != 0
                    ? MathF.Round(
                        this.UpvoteCount/((float)this.UpvoteCount + this.DownvoteCount)*5f, 2
                        )
                    : 0f,
            Pages = pages
        };
    }
}

#pragma warning restore CS8618
