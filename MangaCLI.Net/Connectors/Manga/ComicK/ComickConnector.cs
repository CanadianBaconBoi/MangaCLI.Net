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

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MangaCLI.Net.Connectors.Manga.ComicK.Models;
using MangaCLI.Net.Models;

namespace MangaCLI.Net.Connectors.Manga.ComicK;

public class ComickConnector : IConnector
{
    internal static readonly Uri BaseApiUrl = new("https://api.comick.fun");
    internal static readonly Uri BaseImageUrl = new("https://meo.comick.pictures");

    private readonly HttpClient _httpClient = new();

    public ComickConnector()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MangaCLI.Net Downloader");
    }

    public HttpClient GetClient() => _httpClient;

    IEnumerable<IComic> IConnector.SearchComics(string searchQuery) => SearchComics(searchQuery);

    private IEnumerable<ComickComic> SearchComics(string searchQuery) =>
        _httpClient.GetFromJsonAsync(
            BaseApiUrl.Combine($"/v1.0/search/?type=comic&page=1&limit=50&tachiyomi=true&sort=view&showall=false&q={searchQuery}"),
            ComickJsonContext.Default.ComickComicArray
        ).GetAwaiter().GetResult() ?? [];
}

[JsonSerializable(typeof(ComickPage))]
[JsonSerializable(typeof(ComickPage[]))]
[JsonSerializable(typeof(ComickComic))]
[JsonSerializable(typeof(ComickChapter))]
[JsonSerializable(typeof(ComickChapters))]
[JsonSerializable(typeof(ComickChapterWrapper))]
[JsonSerializable(typeof(ComickChapterWrapper.ComickFatChapter))]
[JsonSerializable(typeof(ComickComicInfo))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo))]
[JsonSerializable(typeof(ComickComicInfo.Artist))]
[JsonSerializable(typeof(ComickComicInfo.Author))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickComics))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickComics.ComicCategoryWrapper))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickComics.ComicCategoryWrapper.ComicCategoryType))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickComics.PublisherWrapper))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickComics.PublisherWrapper.PublisherType))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickContentRating))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickCover))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickGenreWrapper))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickGenreWrapper.ComickGenreType))]
[JsonSerializable(typeof(ComickComicInfo.ComicInfo.ComickTitle))]
[JsonSerializable(typeof(ComickComic[]))]
[JsonSerializable(typeof(ComickChapter[]))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<ComickComicInfo.ComicInfo.ComickTitle>))]
[JsonSerializable(typeof(List<ComickComicInfo.ComicInfo.ComickGenreWrapper>))]
internal partial class ComickJsonContext : JsonSerializerContext;