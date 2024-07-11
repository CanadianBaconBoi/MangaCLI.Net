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
    private ComickComicInfo? _comicInfo;

    [JsonIgnore]
    public ComickComicInfo ComicInfo
    {
        get
        {
            if(_comicInfo == null)
            {
                var comicInfoUrl = ComickConnector.BaseApiUrl.Combine($"/comic/{Slug}?tachiyomi=true");

                _comicInfo = MangaCli.Connector.GetClient().GetFromJsonAsync<ComickComicInfo>(
                    comicInfoUrl,
                    ComickJsonContext.Default.Options
                ).GetAwaiter().GetResult();
            }

            return _comicInfo!;
        }
    }
    
    IEnumerable<IChapter> IComic.GetChapters(string language) => GetChapters(language);
    public IEnumerable<ComickChapter> GetChapters(string language)
    {
        var chaptersUrl = ComickConnector.BaseApiUrl.Combine($"/comic/{Identifier}/chapters?lang={language}&chap-order=1&limit=50");
        ComickChapters? chapters;
        var x = 0;
        var page = 1;
        do
        {
            chapters = MangaCli.Connector.GetClient().GetFromJsonAsync<ComickChapters>(
                chaptersUrl.CombineRaw($"&page={page++}"),
                ComickJsonContext.Default.Options
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